using Core.Abstractions;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using Shared.Result;

namespace FileService.Core.UseCases.Queries.GetFile;

public record GetFileQuery(Guid MediaAssetId) : IQuery<Result<GetFileResponse, Error>>;