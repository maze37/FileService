namespace FileService.Contracts.Dtos;

public record CompleteUploadResponse(
    Guid AssetId,
    string Status,
    long SizeBytes,
    string ContentType,
    string ETag);