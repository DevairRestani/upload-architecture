using Microsoft.AspNetCore.Mvc;
using upload_architecture.Data;
using upload_architecture.DTOs;
using upload_architecture.Model;
using upload_architecture.Services;
using Microsoft.EntityFrameworkCore;
using upload_architecture.Enums;

namespace upload_architecture.Controllers;

[ApiController]
[Route("files")]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly BlobStorageService _blobStorageService;

    public FilesController(AppDbContext context, BlobStorageService blobStorageService)
    {
        _context = context;
        _blobStorageService = blobStorageService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FileDetailsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<FileDetailsResponse>>> ListFiles()
    {
        var files = await _context.Files
            .Where(f => f.DeletedAt == null)
            .OrderByDescending(f => f.CreatedAt)
            .Take(100)
            .ToListAsync();

        return Ok(files.Select(ToDetailsResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FileDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileDetailsResponse>> GetFile(Guid id)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id && f.DeletedAt == null);

        if (file is null)
        {
            return NotFound("File not found.");
        }

        return Ok(ToDetailsResponse(file));
    }

    [HttpPost("upload-url")]
    [ProducesResponseType(typeof(CreateUploadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateUploadUrlResponse>> CreateUploadUrl(
        [FromBody] CreateUploadUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest("FileName is required");
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            return BadRequest("ContentType is required");
        }

        if (request.SizeBytes <= 0)
        {
            return BadRequest("SizeBytes must be greater than zero.");
        }

        var fileId = Guid.NewGuid();
        var extension = Path.GetExtension(request.FileName);
        var expiresAt = DateTime.UtcNow.AddMinutes(10);

        var blobPath = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{fileId}{extension}";

        var file = new FileRecord
        {
            Id = fileId,
            OriginalName = request.FileName,
            BlobPath = blobPath,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            UploadUrlExpiresAt = expiresAt,
            Status = FileUploadStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        
        _context.Files.Add(file);
        await _context.SaveChangesAsync();
        
        
        return Ok(new CreateUploadUrlResponse
        {
            Id = fileId,
            BlobPath = blobPath,
            ExpiresAt =  expiresAt,
            UploadUrl = _blobStorageService.GenerateUploadUrl(blobPath, expiresAt),
        });
    }
    
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(CompleteUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompleteUploadResponse>> CompleteUpload(Guid id)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);

        if (file is null)
            return NotFound("File not found.");

        if (file.UploadUrlExpiresAt < DateTime.UtcNow)
        {
            file.Status = FileUploadStatus.Expired;
            file.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return BadRequest("Upload URL expired.");
        }

        var blobExists = await _blobStorageService.ExistsAsync(file.BlobPath);

        if (!blobExists)
            return BadRequest("Blob was not uploaded.");
        
        var blobLength = await _blobStorageService.GetLengthAsync(file.BlobPath);

        if (blobLength != file.SizeBytes)
        {
            file.Status = FileUploadStatus.Failed;
            file.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return BadRequest("Blob length does not match.");
        }

        file.Status = FileUploadStatus.Uploaded;
        file.ScanStatus = FileScanStatus.NotScanned;
        file.UploadedAt = DateTime.UtcNow;
        file.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new CompleteUploadResponse
        {
            FileId = file.Id,
            Status = file.Status.ToString()
        });
    }

    [HttpPost("{id:guid}/download-url")]
    [ProducesResponseType(typeof(CreateDownloadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateDownloadUrlResponse>> CreateDownloadUrl(Guid id)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == id);

        if (file is null)
        {
            return NotFound("File not found.");
        }

        if (file.DeletedAt is not null)
        {
            return BadRequest("File is deleted.");
        }

        if (file.Status != FileUploadStatus.Uploaded && file.Status != FileUploadStatus.Validated || file.ScanStatus != FileScanStatus.Clean)
        {
            return BadRequest("File is not available for download.");
        }

        if (file.ScanStatus == FileScanStatus.Infected)
        {
            return BadRequest("File is infected.");
        }
        
        var blobExists = await _blobStorageService.ExistsAsync(file.BlobPath);

        if (!blobExists)
        {
            return BadRequest("Blob does not exist.");
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        var downloadUrl = _blobStorageService.GenerateDownloadUrl(
            file.BlobPath,
            expiresAt
        );

        return Ok(new CreateDownloadUrlResponse
        {
            FileId = file.Id,
            DownloadUrl =  downloadUrl,
            ExpiresAt =  expiresAt,
        });
    }

    private static FileDetailsResponse ToDetailsResponse(FileRecord file)
    {
        return new FileDetailsResponse
        {
            Id = file.Id,
            OriginalName = file.OriginalName,
            BlobPath = file.BlobPath,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            Status = file.Status.ToString(),
            ScanStatus = file.ScanStatus.ToString(),
            UploadUrlExpiresAt = file.UploadUrlExpiresAt,
            UploadedAt = file.UploadedAt,
            ScannedAt = file.ScannedAt,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt
        };
    }
}
