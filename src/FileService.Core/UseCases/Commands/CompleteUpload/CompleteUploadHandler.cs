using Core.Abstractions;
using Core.Database;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace FileService.Core.UseCases.Commands.CompleteUpload;

public class CompleteUploadHandler : ICommandHandler<CompleteUploadCommand, CompleteUploadResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<CompleteUploadHandler> _logger;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IS3Provider _s3Provider;

    public CompleteUploadHandler(
        ITransactionManager transactionManager,
        ILogger<CompleteUploadHandler> logger,
        IMediaAssetRepository mediaAssetRepository,
        IS3Provider s3Provider)
    {
        _transactionManager = transactionManager;
        _logger = logger;
        _mediaAssetRepository = mediaAssetRepository;
        _s3Provider = s3Provider;
    }

    public async Task<Result<CompleteUploadResponse, Error>> HandleAsync(
        CompleteUploadCommand command,
        CancellationToken cancellationToken)
    {
        var assetResult = await _mediaAssetRepository.GetByIdAsync(command.MediaAssetId, cancellationToken);
        if (assetResult.IsFailure)
            return assetResult.Error;

        var asset = assetResult.Value;

        if (asset.Status != MediaStatus.UPLOADING)
        {
            return Error.Conflict("media.asset.cannot.complete", $"Нельзя завершить загрузку: текущий статус {asset.Status}");
        }
        
        string bucket = asset.AssetType.ToBucketName();

        var metadataResult = await _s3Provider.GetObjectMetadataAsync(
            asset.StorageKey,
            cancellationToken);

        if (metadataResult.IsFailure)
        {
            _logger.LogWarning("Completion failed for asset {AssetId}: object not found in storage. {Error}", asset.Id, metadataResult.Error);

            return metadataResult.Error;
        }

        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        using var transactionScope = transactionResult.Value;

        var completeResult = asset.MarkUploaded();
        
        if (completeResult.IsFailure)
        {
            _logger.LogWarning("Completion validation failed for asset {AssetId}: {Error}", asset.Id, completeResult.Error);
            return completeResult.Error;
        }

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        var commitResult = transactionScope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;

        return new CompleteUploadResponse(
            asset.Id,
            asset.Status.ToString(),
            metadataResult.Value.SizeBytes,
            metadataResult.Value.ContentType,
            metadataResult.Value.ETag);
    }
}