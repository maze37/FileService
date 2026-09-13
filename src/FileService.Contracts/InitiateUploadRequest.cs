namespace FileService.Contracts;

public record InitiateUploadRequest(
    string FileName, 
    string ContentType,
    long FileSize,
    string Context,
    Guid EntityId,
    string AssetType);