namespace FileService.Contracts.Dtos;

public record GetFilesByTargetEntityResponse(IReadOnlyList<FileDto> Files);