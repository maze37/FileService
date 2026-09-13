using Core.Abstractions;

namespace FileService.Core.UseCases.Commands.InitiateUpload;

public record InitiateUploadCommand(
    string FileName, 
    string ContentType,
    long FileSize,
    string Context,
    Guid EntityId,
    string AssetType) : ICommand;