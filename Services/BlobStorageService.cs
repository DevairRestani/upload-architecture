using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace upload_architecture.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly Uri? _publicEndpoint;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage");
        var containerName = configuration["BlobStorage:ContainerName"];
        var publicEndpoint = configuration["BlobStorage:PublicEndpoint"];

        _containerClient = new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists();

        _publicEndpoint = string.IsNullOrWhiteSpace(publicEndpoint)
            ? null
            : new Uri(publicEndpoint.TrimEnd('/'));
    }

    public string GenerateUploadUrl(string blobPath, DateTimeOffset expiresAt)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
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
        var blobClient = _containerClient.GetBlobClient(blobPath);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
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
        var blobClient = _containerClient.GetBlobClient(blobPath);
        
        var exists = await blobClient.ExistsAsync();
        if (!exists)
        {
            throw new FileNotFoundException($"Blob {blobPath} not found");
        }
        
        return await blobClient.OpenReadAsync();
    }

    public async Task<bool> ExistsAsync(string blobPath)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);

        return await blobClient.ExistsAsync();
    }

    public async Task<long> GetLengthAsync(string blobPath)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);

        var properties = await blobClient.GetPropertiesAsync();
        
        return properties.Value.ContentLength;
    }
}
