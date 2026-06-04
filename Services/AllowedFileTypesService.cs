namespace upload_architecture.Services;

public class AllowedFileTypesService
{
    private readonly HashSet<string> _allowedMimeTypes;
    private readonly HashSet<string> _allowedExtensions;

    public AllowedFileTypesService(IConfiguration configuration)
    {
        _allowedMimeTypes = configuration
                                .GetSection("Upload:AllowedMimeTypes")
                                .Get<string[]>()?
                                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        _allowedExtensions = configuration
                                 .GetSection("Upload:AllowedExtensions")
                                 .Get<string[]>()?
                                 .ToHashSet(StringComparer.OrdinalIgnoreCase) 
                             ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
    
    public bool IsAllowedMimeType(string mimeType) => _allowedMimeTypes.Contains(mimeType);
    
    public bool IsAllowedExtension(string extension) => _allowedExtensions.Contains(extension);
}