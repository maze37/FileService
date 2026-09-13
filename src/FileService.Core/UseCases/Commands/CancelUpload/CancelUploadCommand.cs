using Core.Abstractions;

namespace FileService.Core.UseCases.Commands.CancelUpload;

public record CancelUploadCommand(Guid FileId) : ICommand;