namespace FileService.Contracts;

public record InitiateUploadResponse(
    Guid AssetId, 
    string UploadUrl,
    DateTimeOffset ExpiresWhen, 
    int RequiredHeaders);