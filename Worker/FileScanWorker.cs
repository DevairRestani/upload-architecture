using Microsoft.EntityFrameworkCore;
using upload_architecture.Data;
using upload_architecture.Enums;
using upload_architecture.Services;

namespace upload_architecture.Worker;

public class FileScanWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FileScanWorker> _logger;
    private readonly long _clamAvStreamMaxLengthBytes;
    
    public FileScanWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<FileScanWorker> logger,
        IConfiguration configuration)
        {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _clamAvStreamMaxLengthBytes = configuration.GetValue<long?>("ClamAv:StreamMaxLengthBytes") ?? 104_857_600;
        }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ScanNextFile(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ScanNextFile(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blobStorageService = scope.ServiceProvider.GetRequiredService<BlobStorageService>();
        var clamAvService = scope.ServiceProvider.GetRequiredService<ClamAvService>();
        
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var file = await context.Files
            .FromSqlRaw("""
                        SELECT * FROM files
                        WHERE status = {0}
                        AND scan_status = {1}
                        AND deleted_at IS NULL
                        ORDER BY created_at DESC
                        LIMIT 1
                        FOR UPDATE SKIP LOCKED
                        """, 
                FileUploadStatus.Uploaded,
                FileScanStatus.NotScanned)
            .FirstOrDefaultAsync(cancellationToken);

        if (file == null) return;
        
        file.ScanStatus = FileScanStatus.Scanning;
        file.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        try
        {
            if (file.SizeBytes > _clamAvStreamMaxLengthBytes)
            {
                _logger.LogWarning(
                    "File {FileId} exceeds ClamAV INSTREAM limit. SizeBytes={SizeBytes}, LimitBytes={LimitBytes}",
                    file.Id,
                    file.SizeBytes,
                    _clamAvStreamMaxLengthBytes);

                file.ScanStatus = FileScanStatus.Failed;
                file.Status = FileUploadStatus.Failed;
                file.ScannedAt = DateTime.UtcNow;
                file.UpdatedAt = DateTime.UtcNow;

                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            await using var blobStream = await blobStorageService.OpenReadTempAsync(file.TempBlobPath);
            
            var scanResult = await clamAvService.ScanAsync(blobStream, cancellationToken);

            file.ScanStatus = scanResult switch
            {
                ClamAvScanResult.Clean => FileScanStatus.Clean,
                ClamAvScanResult.Infected => FileScanStatus.Infected,
                ClamAvScanResult.ExceededSizeLimit => FileScanStatus.Failed,
                _ => FileScanStatus.Failed
            };

            if (file.ScanStatus == FileScanStatus.Clean)
            {
                await blobStorageService.PromoteToFinalAsync(
                    file.TempBlobPath,
                    file.BlobPath,
                    file.ContentType);

                file.Status = FileUploadStatus.Validated;
            }
            else
            {
                file.Status = FileUploadStatus.Failed;
            }
            
            file.ScannedAt =  DateTime.UtcNow;
            file.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            
            file.ScanStatus = FileScanStatus.Failed;
            file.Status = FileUploadStatus.Failed;
            file.UpdatedAt = DateTime.UtcNow;
            
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
