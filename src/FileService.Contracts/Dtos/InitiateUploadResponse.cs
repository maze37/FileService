namespace FileService.Contracts.Dtos;

public record InitiateUploadResponse(
    Guid AssetId, 
    string UploadUrl,
    DateTimeOffset ExpiresWhen, 
    int RequiredHeaders);