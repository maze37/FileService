namespace FileService.Contracts;

public record GetFilesByTargetEntityResponse(IReadOnlyList<FileDto> Files);