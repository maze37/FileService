using Core.Abstractions;
using FileService.Contracts;

namespace FileService.Core.UseCases.Commands.InitiateUpload;

public record InitiateUploadCommand(InitiateUploadRequest Request) : ICommand;