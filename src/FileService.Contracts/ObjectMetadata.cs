namespace FileService.Contracts;

public record ObjectMetadata(
    string ETag,
    string ContentType,
    long SizeBytes,
    DateTime LastModified);