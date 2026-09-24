using Core.Abstractions;
using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Core.Abstractions;
using FileService.Domain;
using FileService.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using SharedKernel;

namespace FileService.Core.UseCases.Queries.GetFilesByTargetEntity;

public class GetFilesByTargetEntityHandler : IQueryHandlerWithResult<GetFilesByTargetEntityQuery, GetFilesByTargetEntityResponse>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly HybridCache _cache;

    public GetFilesByTargetEntityHandler(
        IMediaAssetRepository mediaAssetRepository,
        HybridCache cache)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _cache = cache;
    }

    public async Task<Result<GetFilesByTargetEntityResponse, Error>> HandleAsync(
        GetFilesByTargetEntityQuery query,
        CancellationToken cancellationToken)
    {
        if (!MediaOwner.AllowedContexts.Contains(query.Context.Trim().ToLowerInvariant()))
            return Error.Validation("media.owner.context.invalid", $"Неизвестный контекст: {query.Context}");
        
        var assets = await _mediaAssetRepository.GetByOwnerAsync(
            query.Context,
            query.EntityId,
            cancellationToken);

        var files = assets
            .Where(a => a.Status != MediaStatus.DELETED)
            .Select(a => new FileDto(
                a.Id,
                a.MediaData.FileName,
                a.MediaData.ContentType,
                a.MediaData.FileSize,
                a.Status.ToString(),
                a.AssetType.ToString()))
            .ToList();

        return new GetFilesByTargetEntityResponse(files);
    }
}