namespace FileService.Contracts;

public record GetFileResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    string Status,
    string? DownloadUrl);