using System.Net.Sockets;
using System.Text;

namespace upload_architecture.Services;

public enum ClamAvScanResult
{
    Clean,
    Infected,
    ExceededSizeLimit,
    Error
}

public class ClamAvService
{
    private readonly string _host;
    private readonly int _port;
    private readonly ILogger<ClamAvService> _logger;
    
    public ClamAvService(IConfiguration configuration, ILogger<ClamAvService> logger)
    {
        _host = configuration["ClamAv:Host"] ?? "localhost";
        _port = int.Parse(configuration["ClamAv:Port"] ?? "3310");
        _logger = logger;
    }

    public async Task<ClamAvScanResult> ScanAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            
            await client.ConnectAsync(_host, _port, cancellationToken);
            
            await using var networkStream = client.GetStream();

            var command = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await networkStream.WriteAsync(command, cancellationToken);
            
            var buffer = new byte[8192];

            while (true)
            {
                var bytesRead = await fileStream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0) break;

                var sizeBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(bytesRead));

                await networkStream.WriteAsync(sizeBytes, cancellationToken);
                await networkStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            var endChunk = BitConverter.GetBytes(0);
            await networkStream.WriteAsync(endChunk, cancellationToken);
            
            var responseBuffer = new byte[4096];
            var responseLength = await networkStream.ReadAsync(responseBuffer, cancellationToken);
            
            var response = Encoding.UTF8.GetString(responseBuffer, 0, responseLength);

            if (response.Contains("OK", StringComparison.OrdinalIgnoreCase))
            {
                return ClamAvScanResult.Clean;
            }

            if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return ClamAvScanResult.Infected;
            }

            if (response.Contains("Size limit reached", StringComparison.OrdinalIgnoreCase))
            {
                return ClamAvScanResult.ExceededSizeLimit;
            }

            _logger.LogWarning("Unexpected ClamAV response: {Response}", response);
            return ClamAvScanResult.Error;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClamAV scan failed.");
            return ClamAvScanResult.Error;
        }
    }
}
