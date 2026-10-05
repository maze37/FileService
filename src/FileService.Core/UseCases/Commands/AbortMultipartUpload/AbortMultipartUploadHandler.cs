using Core.Abstractions;
using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace FileService.Core.UseCases.Commands.AbortMultipartUpload;

public class AbortMultipartUploadHandler : ICommandHandler<AbortMultipartUploadCommand, AbortMultipartUploadResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<AbortMultipartUploadHandler> _logger;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IS3Provider _s3Provider;
    
    public AbortMultipartUploadHandler(
        ITransactionManager transactionManager,
        ILogger<AbortMultipartUploadHandler> logger,
        IMediaAssetRepository mediaAssetRepository,
        IS3Provider s3Provider)
    {
        _transactionManager = transactionManager;
        _logger = logger;
        _mediaAssetRepository = mediaAssetRepository;
        _s3Provider = s3Provider;
    }

    public async Task<Result<AbortMultipartUploadResponse, Error>> HandleAsync(
        AbortMultipartUploadCommand command,
        CancellationToken cancellationToken)
    {
        var mediaAssetResult = await _mediaAssetRepository
            .GetByIdAsync(command.Request.MediaAssetId, cancellationToken);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        var mediaAsset = mediaAssetResult.Value;
        
        if (mediaAsset.Status is not (MediaStatus.PENDING or MediaStatus.UPLOADING))
            return Error.Failure("media.asset.invalid_status",
                $"Asset в статусе '{mediaAsset.Status}' нельзя отменить");
        
        if (mediaAsset.UploadId != command.Request.UploadId)
            return Error.Failure("media.asset.upload_id_mismatch", "UploadId не соответствует asset'у");
        
        var abortUploadResult = await _s3Provider.AbortMultipartUploadAsync(
            mediaAsset.StorageKey,
            command.Request.UploadId,
            cancellationToken);
        if (abortUploadResult.IsFailure)
            return abortUploadResult.Error;
        
        var transaction = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transaction.IsFailure)
            return transaction.Error;
        
        _mediaAssetRepository.Remove(mediaAsset);
        
        var commitResult = await _transactionManager.CommitTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
            return commitResult.Error;
        
        _logger.LogInformation("Ассет {MediaAssetId} полностью удален.", mediaAsset.Id);
        
        return new AbortMultipartUploadResponse(mediaAsset.Id);
    }
}