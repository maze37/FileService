using Core.Abstractions;
using FileService.Contracts.Dtos;

namespace FileService.Core.UseCases.Queries.CheckMediaAssetExists;

public record CheckMediaAssetExistsQuery(Guid AssetId) : IQuery<CheckMediaAssetExistsAndReadyResponse>;