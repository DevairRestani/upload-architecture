namespace upload_architecture.DTOs;

public class FileDetailsResponse
{
    public Guid Id { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ScanStatus { get; set; } = string.Empty;
    public DateTime? UploadUrlExpiresAt { get; set; }
    public DateTime? UploadedAt { get; set; }
    public DateTime? ScannedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
