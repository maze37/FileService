using Core.Abstractions;
using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using SharedKernel;

namespace FileService.Core.UseCases.Queries.GetFile;

public record GetFileQuery(GetFileRequest Request) : IQuery<Result<GetFileResponse, Error>>;