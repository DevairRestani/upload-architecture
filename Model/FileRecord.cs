using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using upload_architecture.Enums;

namespace upload_architecture.Model;

[Table("files")]
public class FileRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("original_name")]
    public string OriginalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("blob_path")]
    public string BlobPath { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("temp_blob_path")]
    public string TempBlobPath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [Column("size_bytes")]
    public long SizeBytes { get; set; }

    [Column("status")]
    public FileUploadStatus Status { get; set; } = FileUploadStatus.Pending;

    [Column("scan_status")]
    public FileScanStatus ScanStatus { get; set; } = FileScanStatus.NotScanned;

    [Column("upload_url_expires_at")]
    public DateTime? UploadUrlExpiresAt { get; set; }

    [Column("uploaded_at")]
    public DateTime? UploadedAt { get; set; }

    [Column("scanned_at")]
    public DateTime? ScannedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
