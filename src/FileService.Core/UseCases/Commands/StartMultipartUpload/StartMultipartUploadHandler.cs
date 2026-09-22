using Core.Abstractions;
using Core.Database;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.Abstractions;
using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace FileService.Core.UseCases.Commands.StartMultipartUpload;

public class StartMultipartUploadHandler : ICommandHandler<StartMultipartUploadCommand, StartMultipartUploadResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<StartMultipartUploadHandler> _logger;
    private readonly IS3Provider _s3Provider;
    private readonly IChunkSizeCalculator _chunkSizeCalculator;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    
    public StartMultipartUploadHandler(
        ITransactionManager transactionManager,
        ILogger<StartMultipartUploadHandler> logger,
        IS3Provider s3Provider,
        IChunkSizeCalculator chunkSizeCalculator,
        IMediaAssetRepository mediaAssetRepository)
    {
        _transactionManager = transactionManager;
        _logger = logger;
        _s3Provider = s3Provider;
        _chunkSizeCalculator = chunkSizeCalculator;
        _mediaAssetRepository = mediaAssetRepository;
    }

    public async Task<Result<StartMultipartUploadResponse, Error>> HandleAsync(
        StartMultipartUploadCommand command,
        CancellationToken cancellationToken)
    {
        var fileNameResult = FileName.Create(command.Request.FileName);
        if (fileNameResult.IsFailure)
            return fileNameResult.Error;

        var contentTypeResult = ContentType.Create(command.Request.ContentType);
        if (contentTypeResult.IsFailure)
            return contentTypeResult.Error;

        var fileSizeResult = FileSize.Create(command.Request.FileSize);
        if (fileSizeResult.IsFailure)
            return fileSizeResult.Error;
        
        Result<(int ChunkSize, int TotalChunks), Error> chunkCalculationResult = _chunkSizeCalculator
            .CalculateChunkSize(fileSizeResult.Value);
        if (chunkCalculationResult.IsFailure)
            return chunkCalculationResult.Error;
        
        var mediaDataResult = MediaData.Create(
            fileNameResult.Value,
            contentTypeResult.Value,
            fileSizeResult.Value,
            chunkCalculationResult.Value.TotalChunks);
        if (mediaDataResult.IsFailure)
            return mediaDataResult.Error;
        
        var mediaOwnerResult = MediaOwner.Create(command.Request.Context, command.Request.EntityId);
        if (mediaOwnerResult.IsFailure)
            return mediaOwnerResult.Error;
        
        var assetType = AssetTypeExtensions.ToAssetType(command.Request.AssetType);

        string prefix = command.Request.Context.ToLower();
        string bucketName = assetType.ToBucketName();

        var storageKeyResult = StorageKey.CreateNew(bucketName, prefix);
        if (storageKeyResult.IsFailure)
            return storageKeyResult.Error;
        
        var mediaAssetResult = MediaAsset.CreateForUpload(
            assetType,
            mediaDataResult.Value,
            mediaOwnerResult.Value,
            storageKeyResult.Value);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;

        var mediaAsset = mediaAssetResult.Value;
        
        mediaAsset.BeginUpload();
        
        var startUploadResult = await _s3Provider.StartMultipartUploadAsync(
            storageKeyResult.Value,
            contentTypeResult.Value, 
            cancellationToken);
        if (startUploadResult.IsFailure)
            return startUploadResult.Error;
        
        var chunkUploadUrlsResult = await _s3Provider.GenerateAllChunksUploadUrlsAsync(
            storageKeyResult.Value,
            startUploadResult.Value,
            chunkCalculationResult.Value.TotalChunks,
            cancellationToken);
        if (chunkUploadUrlsResult.IsFailure)
            return chunkUploadUrlsResult.Error;
        
        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        using var transactionScope = transactionResult.Value;
        
        var attachResult = mediaAsset.AttachUploadId(startUploadResult.Value);
        if (attachResult.IsFailure)
            return attachResult.Error;
        
        _mediaAssetRepository.Add(mediaAsset);
        
        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        var commitResult = transactionScope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;

        _logger.LogInformation("Media Asset started uploading: {MediaAssetId} with key: {StorageKey}",
            mediaAssetResult.Value.Id,
            mediaAssetResult.Value.StorageKey.Value);

        return new StartMultipartUploadResponse(
            mediaAssetResult.Value.Id, 
            startUploadResult.Value,
            chunkUploadUrlsResult.Value,
            chunkCalculationResult.Value.ChunkSize);
    }
}
