using ICommand = Core.Abstractions.ICommand;

namespace FileService.Core.UseCases.Commands.CompleteUpload;

public record CompleteUploadCommand(Guid MediaAssetId) : ICommand;