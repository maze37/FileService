namespace FileService.Contracts.Dtos;

public record InitiateUploadRequest(
    string FileName, 
    string ContentType,
    long FileSize,
    string Context,
    Guid EntityId,
    string AssetType);