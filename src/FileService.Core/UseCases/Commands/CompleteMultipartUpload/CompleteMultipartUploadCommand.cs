using Core.Abstractions;
using FileService.Contracts;
using FileService.Contracts.Dtos;

namespace FileService.Core.UseCases.Commands.CompleteMultipartUpload;

public record CompleteMultipartUploadCommand(CompleteMultipartUploadRequest Request) : ICommand;