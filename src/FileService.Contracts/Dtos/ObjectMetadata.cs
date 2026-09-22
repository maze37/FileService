namespace FileService.Contracts.Dtos;

public record ObjectMetadata(
    string ETag,
    string ContentType,
    long SizeBytes,
    DateTime LastModified);