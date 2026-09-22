namespace FileService.Contracts.Dtos;

public record StartMultipartUploadRequest(
    string FileName,
    string AssetType,
    string ContentType,
    long FileSize,
    string Context,
    Guid EntityId);