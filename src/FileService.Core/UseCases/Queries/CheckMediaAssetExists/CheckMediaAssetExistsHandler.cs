using Core.Abstractions;
using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FileService.Core.UseCases.Queries.CheckMediaAssetExists;

public class CheckMediaAssetExistsAndReadyHandler : IQueryHandlerWithResult<CheckMediaAssetExistsQuery, CheckMediaAssetExistsAndReadyResponse>
{
    private readonly IReadDbContext _readDbContext;

    public CheckMediaAssetExistsAndReadyHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public async Task<Result<CheckMediaAssetExistsAndReadyResponse, Error>> HandleAsync(
        CheckMediaAssetExistsQuery query,
        CancellationToken cancellationToken)
    {
        var asset = await _readDbContext.MediaAssetsRead
            .FirstOrDefaultAsync(a => a.Id == query.AssetId, cancellationToken);

        if (asset is null)
            return new CheckMediaAssetExistsAndReadyResponse(false, false, "Unavailable");

        return new CheckMediaAssetExistsAndReadyResponse(
            AssetExists: true,
            IsReady: asset.Status == MediaStatus.UPLOADED,
            ContentType: asset.StorageMetadata.ContentType);
    }
}