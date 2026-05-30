namespace upload_architecture.DTOs;

public class CreateUploadUrlRequest
{
    public string FileName { get; set; } = String.Empty;
    public string ContentType { get; set; } = String.Empty;
    public long SizeBytes { get; set; }
}