namespace FileService.Contracts;

public record CompleteUploadResponse(
    Guid AssetId,
    string Status,
    long SizeBytes,
    string ContentType,
    string ETag);