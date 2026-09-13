namespace FileService.Contracts;

public record FileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status,
    string AssetType);