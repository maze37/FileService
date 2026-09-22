using Core.Abstractions;
using FileService.Contracts;
using FileService.Contracts.Dtos;

namespace FileService.Core.UseCases.Commands.StartMultipartUpload;

public record StartMultipartUploadCommand(StartMultipartUploadRequest Request) : ICommand;