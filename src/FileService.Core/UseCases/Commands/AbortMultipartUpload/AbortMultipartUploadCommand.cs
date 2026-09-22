using Core.Abstractions;
using FileService.Contracts;
using FileService.Contracts.Dtos;

namespace FileService.Core.UseCases.Commands.AbortMultipartUpload;

public record AbortMultipartUploadCommand(AbortMultipartUploadRequest  Request) : ICommand;