using Core.Abstractions;
using FileService.Contracts;
using FileService.Contracts.Dtos;

namespace FileService.Core.UseCases.Commands.InitiateUpload;

public record InitiateUploadCommand(InitiateUploadRequest Request) : ICommand;