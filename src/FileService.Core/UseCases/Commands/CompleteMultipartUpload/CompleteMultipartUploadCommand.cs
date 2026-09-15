using Core.Abstractions;
using FileService.Contracts;

namespace FileService.Core.UseCases.Commands.CompleteMultipartUpload;

public record CompleteMultipartUploadCommand(CompleteMultipartUploadRequest Request) : ICommand;