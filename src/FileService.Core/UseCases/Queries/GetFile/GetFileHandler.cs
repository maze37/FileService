using Core.Abstractions;
using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using FileService.Infrastructure.S3;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace FileService.Core.UseCases.Queries.GetFile;

public class GetFileHandler : IQueryHandlerWithResult<GetFileQuery, GetFileResponse>
{
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IS3Provider _s3Provider;
    private readonly ILogger<GetFileHandler> _logger;
    private readonly HybridCache _cache;
    private readonly FileStorageOptions _fileStorageOptions;

    public GetFileHandler(
        IMediaAssetRepository mediaAssetRepository,
        IS3Provider s3Provider,
        ILogger<GetFileHandler> logger,
        HybridCache cache,
        IOptions<FileStorageOptions> fileStorageOptions)
    {
        _mediaAssetRepository = mediaAssetRepository;
        _s3Provider = s3Provider;
        _logger = logger;
        _cache = cache;
        _fileStorageOptions = fileStorageOptions.Value;
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
        
        Error? error = null;
        
        string? url = await _cache.GetOrCreateAsync<string?>(
            asset.StorageKey.Value,
            async token =>
            {
                _logger.LogInformation("CACHE MISS: генерирую ссылку для {AssetId}", asset.Id);
                
                var result = await _s3Provider.GenerateDownloadUrlAsync(asset.StorageKey, token);
                if (result.IsSuccess)
                    return result.Value;

                error = result.Error;
                return null;
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(_fileStorageOptions.DownloadUrlExpirationHours) * 0.6,
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            },
            cancellationToken: cancellationToken);

        if (url is null)
        {
            await _cache.RemoveAsync(asset.StorageKey.Value, cancellationToken);

            _logger.LogError(
                "Не удалось сгенерировать ссылку для скачивания файла {AssetId}: {Error}",
                asset.Id, error);

            return error ?? Error.Failure("s3.download_url.failed", "Не удалось получить ссылку");
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
            url);
    }
}