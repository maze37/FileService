using Core.Abstractions;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace FileService.Core.UseCases.Queries.GetFile;

public class GetFileHandler : IQueryHandlerWithResult<GetFileQuery, GetFileResponse>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IS3Provider _s3Provider;
    private readonly ILogger<GetFileHandler> _logger;

    public GetFileHandler(
        IMediaAssetRepository mediaAssetRepository,
        IS3Provider s3Provider,
        ILogger<GetFileHandler> logger)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _s3Provider = s3Provider;
        _logger = logger;
    }

    public async Task<Result<GetFileResponse, Error>> HandleAsync(
        GetFileQuery query,
        CancellationToken cancellationToken)
    {
        var assetResult = await _mediaAssetRepository.GetByIdAsync(query.Request.MediaAssetId, cancellationToken);
        if (assetResult.IsFailure)
            return assetResult.Error;

        var asset = assetResult.Value;

        if (asset.Status != MediaStatus.UPLOADED)
        {
            return Error.Conflict(
                "media.asset.not_ready",
                $"Файл не готов к получению: текущий статус {asset.Status}");
        }

        string bucket = asset.AssetType.ToBucketName();

        var downloadUrlResult = await _s3Provider.GenerateDownloadUrlAsync(
            asset.StorageKey,
            cancellationToken);

        if (downloadUrlResult.IsFailure)
        {
            _logger.LogError(
                "Не удалось сгенерировать ссылку для скачивания файла {AssetId}: {Error}",
                asset.Id, downloadUrlResult.Error);

            return downloadUrlResult.Error;
        }

        return new GetFileResponse(
            asset.Id,
            asset.MediaData.FileName,
            asset.MediaData.ContentType,
            asset.MediaData.FileSize,
            asset.AssetType.ToString(),
            asset.Status.ToString(),
            asset.MediaOwner.Context,
            asset.MediaOwner.EntityId,
            downloadUrlResult.Value);
    }
}