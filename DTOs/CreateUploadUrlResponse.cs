namespace upload_architecture.DTOs;

public class CreateUploadUrlResponse
{
    public Guid Id { get; set; }
    public string UploadUrl { get; set; } = String.Empty;
    public DateTime ExpiresAt { get; set; } 
}
