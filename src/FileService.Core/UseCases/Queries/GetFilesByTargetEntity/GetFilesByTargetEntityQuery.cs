using Core.Abstractions;
using FileService.Contracts;

namespace FileService.Core.UseCases.Queries.GetFilesByTargetEntity;

public record GetFilesByTargetEntityQuery(string Context, Guid EntityId) : IQuery<GetFilesByTargetEntityResponse>;