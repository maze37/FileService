using Core.Abstractions;
using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using IntegrationEvents.Files.Events;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace FileService.Core.UseCases.Commands.DeleteFile;

public class DeleteFileHandler : ICommandHandler<DeleteFileCommand, DeleteFileResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IS3Provider _s3Provider;
    private readonly ILogger<DeleteFileHandler> _logger;
    private readonly HybridCache _cache;
    private readonly IOutboxService _outbox;

    public DeleteFileHandler(
        ITransactionManager transactionManager,
        IMediaAssetRepository mediaAssetRepository,
        IS3Provider s3Provider,
        ILogger<DeleteFileHandler> logger,
        HybridCache cache,
        IOutboxService outbox)
    {
        _transactionManager = transactionManager;
        _mediaAssetRepository = mediaAssetRepository;
        _s3Provider = s3Provider;
        _logger = logger;
        _cache = cache;
        _outbox = outbox;
    }

    public async Task<Result<DeleteFileResponse, Error>> HandleAsync(
        DeleteFileCommand command,
        CancellationToken cancellationToken)
    {
        var assetResult = await _mediaAssetRepository.GetByIdAsync(command.FileId, cancellationToken);
        if (assetResult.IsFailure)
            return assetResult.Error;

        var asset = assetResult.Value;
        
        if (asset.Status != MediaStatus.UPLOADED)
            return Error.Conflict(
                "media.asset.cannot.delete",
                $"Нельзя удалить файл: текущий статус {asset.Status}");
        
        var transaction = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transaction.IsFailure)
            return transaction.Error;
        
        var markResult = asset.MarkAsDeleted();
        if (markResult.IsFailure)
            return markResult.Error;
        
        await _outbox.PublishAsync(new AssetDeleted(
            AssetId: asset.Id,
            EntityId: asset.MediaOwner.EntityId,
            EntityType: asset.MediaOwner.Context,
            AssetType: asset.AssetType.ToString(),
            OccurredAt: DateTimeOffset.UtcNow));
        
        var commitResult = await _transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
            return commitResult.Error;
        
        try
        {
            _logger.LogInformation("Удаляю из кэша ассет {AssetId}", asset.Id);
            
            await _cache.RemoveAsync(asset.StorageKey.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось сбросить кэш ссылки для файла {AssetId}", asset.Id);
        }
        
        var deleteResult = await _s3Provider.DeleteObjectAsync(
            asset.StorageKey,
            cancellationToken);
        
        if (deleteResult.IsFailure)
        {
            _logger.LogWarning(
                "Состояние ассета {AssetId} изменено в БД, но чистка S3 провалена: {Error}",
                asset.Id, deleteResult.Error);
        }
        
        return new DeleteFileResponse(asset.Id);
    }
}