using System.Windows.Input;
using ICommand = Core.Abstractions.ICommand;

namespace FileService.Core.UseCases.Commands.DeleteFile;

public record DeleteFileCommand(Guid FileId) : ICommand;