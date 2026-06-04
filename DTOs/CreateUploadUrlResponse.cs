namespace upload_architecture.DTOs;

public class CreateUploadUrlResponse
{
    public Guid Id { get; set; }
    public string BlobPath { get; set; } = String.Empty;
    public string TempBlobPath { get; set; } = String.Empty;
    public string FinalBlobPath { get; set; } = String.Empty;
    public string UploadUrl { get; set; } = String.Empty;
    public DateTime ExpiresAt { get; set; } 
}
