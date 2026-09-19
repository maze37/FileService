using Core.Abstractions;
using Core.Database;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace FileService.Core.UseCases.Commands.CompleteMultipartUpload;

public class CompleteMultipartUploadHandler : ICommandHandler<CompleteMultipartUploadCommand, CompleteMultipartUploadResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<CompleteMultipartUploadHandler> _logger;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IS3Provider _s3Provider;
    
    public CompleteMultipartUploadHandler(
        ITransactionManager transactionManager,
        ILogger<CompleteMultipartUploadHandler> logger,
        IMediaAssetRepository mediaAssetRepository,
        IS3Provider s3Provider)
    {
        _transactionManager = transactionManager;
        _logger = logger;
        _mediaAssetRepository = mediaAssetRepository;
        _s3Provider = s3Provider;
    }

    public async Task<Result<CompleteMultipartUploadResponse, Error>> HandleAsync(
        CompleteMultipartUploadCommand command,
        CancellationToken cancellationToken)
    {
        var mediaAssetResult = await _mediaAssetRepository
            .GetByAsync(m => m.Id == command.Request.MediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        var mediaAsset = mediaAssetResult.Value;
        
        if (mediaAsset.Status is not MediaStatus.UPLOADING)
            return Error.Failure("media.asset.invalid_status",
                $"Asset в статусе '{mediaAsset.Status}' нельзя завершить");
        
        if (mediaAsset.UploadId != command.Request.UploadId)
            return Error.Failure("media.asset.upload_id_mismatch", "UploadId не соответствует asset'у");

        if (mediaAsset.MediaData.ExpectedChunksCount != command.Request.PartETags.Count)
            return Error.Conflict("media.asset.parts.count.mismatch",
                $"Ожидалось частей: {mediaAsset.MediaData.ExpectedChunksCount}, " +
                $"получено: {command.Request.PartETags.Count}");
        
        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        using var transactionScope = transactionResult.Value;
        
        var s3CompleteResult = await _s3Provider.CompleteMultipartUploadAsync(
            mediaAsset.StorageKey,
            command.Request.UploadId,
            command.Request.PartETags,
            cancellationToken);
        if (s3CompleteResult.IsFailure)
            return s3CompleteResult.Error;
        
        var metadataResult = await _s3Provider.GetObjectMetadataAsync(mediaAsset.StorageKey, cancellationToken);
        if (metadataResult.IsFailure)
            return metadataResult.Error;

        var completeResult = mediaAsset.CompleteUpload(metadataResult.Value);
        if (completeResult.IsFailure)
        {
            // объект в S3 уже есть, но не прошёл проверку - подчищаем
            var cleanupResult = await _s3Provider.DeleteObjectAsync(mediaAsset.StorageKey, cancellationToken);
            if (cleanupResult.IsFailure)
                return cleanupResult.Error;
            
            _logger.LogError("Не удалось удалить невалидный объект {Key} после mismatch: {Error}", 
                mediaAsset.StorageKey.Value, cleanupResult.Error.Messages);
            
            return completeResult.Error;
        }
        
        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        var commitResult = transactionScope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;
        
        _logger.LogInformation("Файл успешно загружен: {MediaAssetId}", mediaAsset.Id);
        
        return new CompleteMultipartUploadResponse(mediaAsset.Id);
    }
}
