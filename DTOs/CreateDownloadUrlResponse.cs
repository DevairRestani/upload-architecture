namespace upload_architecture.DTOs;

public class CreateDownloadUrlResponse
{
    public Guid FileId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}