using Core.Abstractions;
using FileService.Contracts;

namespace FileService.Core.UseCases.Commands.StartMultipartUpload;

public record StartMultipartUploadCommand(StartMultipartUploadRequest Request) : ICommand;