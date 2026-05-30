namespace upload_architecture.DTOs;

public class CompleteUploadResponse
{
    public Guid FileId { get; set; }
    public string Status { get; set; } = string.Empty;
}