namespace FileService.Contracts.Dtos;

public record GetFileResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string AssetType,
    string Status,
    string Context,
    Guid EntityId,
    string? DownloadUrl);