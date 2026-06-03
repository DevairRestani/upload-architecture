using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace upload_architecture.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _tempContainerClient;
    private readonly BlobContainerClient _finalContainerClient;
    private readonly Uri? _publicEndpoint;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage");
        var tempContainerName = configuration["BlobStorage:TempContainerName"];
        var finalContainerName = configuration["BlobStorage:FinalContainerName"];
        var publicEndpoint = configuration["BlobStorage:PublicEndpoint"];

        _tempContainerClient = new BlobContainerClient(connectionString, tempContainerName);
        _tempContainerClient.CreateIfNotExists();
        
        _finalContainerClient = new BlobContainerClient(connectionString, finalContainerName);
        _finalContainerClient.CreateIfNotExists();

        _publicEndpoint = string.IsNullOrWhiteSpace(publicEndpoint)
            ? null
            : new Uri(publicEndpoint.TrimEnd('/'));
    }

    public string CreateUploadBlobPath(Guid fileId, string extension)
    {
        return $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{fileId}{extension}";
    }

    public string GenerateUploadUrl(string blobPath, DateTimeOffset expiresAt)
    {
        var blobClient = _tempContainerClient.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _tempContainerClient.Name,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = expiresAt
        };

        sasBuilder.SetPermissions(
            BlobSasPermissions.Create |
            BlobSasPermissions.Write
        );

        return ToPublicUri(blobClient.GenerateSasUri(sasBuilder));
    }

    public string GenerateDownloadUrl(string blobPath, DateTimeOffset expiresAt)
    {
        var blobClient = _finalContainerClient.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _finalContainerClient.Name,
            BlobName = blobPath,
            Resource = "b",
            ExpiresOn = expiresAt
        };
        
        sasBuilder.SetPermissions(BlobSasPermissions.Read);
        return ToPublicUri(blobClient.GenerateSasUri(sasBuilder));
    }

    private string ToPublicUri(Uri sasUri)
    {
        if (_publicEndpoint is null)
            return sasUri.ToString();

        var publicUri = new UriBuilder(sasUri)
        {
            Scheme = _publicEndpoint.Scheme,
            Host = _publicEndpoint.Host,
            Port = _publicEndpoint.IsDefaultPort ? -1 : _publicEndpoint.Port
        };

        return publicUri.Uri.ToString();
    }

    public async Task<Stream> OpenReadAsync(string blobPath)
    {
        var blobClient = _finalContainerClient.GetBlobClient(blobPath);
        
        var exists = await blobClient.ExistsAsync();
        if (!exists)
        {
            throw new FileNotFoundException($"Blob {blobPath} not found");
        }
        
        return await blobClient.OpenReadAsync();
    }

    public async Task<bool> ExistsOnTempAsync(string blobPath)
    {
        var blobClient = _tempContainerClient.GetBlobClient(blobPath);

        return await blobClient.ExistsAsync();
    }
    
    public async Task<bool> ExistsOnFinalAsync(string blobPath)
    {
        var blobClient = _finalContainerClient.GetBlobClient(blobPath);

        return await blobClient.ExistsAsync();
    }

    public async Task<long?> TryGetLengthAsync(string blobPath)
    {
        var blobClient = _tempContainerClient.GetBlobClient(blobPath);

        try
        {
            var properties = await blobClient.GetPropertiesAsync();
            return properties.Value.ContentLength;
        }
        catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }
}
