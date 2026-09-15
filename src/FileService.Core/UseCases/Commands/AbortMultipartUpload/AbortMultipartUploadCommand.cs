using Core.Abstractions;
using FileService.Contracts;

namespace FileService.Core.UseCases.Commands.AbortMultipartUpload;

public record AbortMultipartUploadCommand(AbortMultipartUploadRequest  Request) : ICommand;