using Core.Abstractions;
using Core.Database;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Abstractions;
using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared.Result;

namespace FileService.Core.UseCases.Commands.InitiateUpload;

public class InitiateUploadHandler : ICommandHandler<InitiateUploadCommand, InitiateUploadResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<InitiateUploadHandler> _logger;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IS3Provider _s3Provider;
    
    public InitiateUploadHandler(
        ITransactionManager transactionManager,
        IDateTimeProvider dateTime,
        ILogger<InitiateUploadHandler> logger,
        IMediaAssetRepository mediaAssetRepository,
        IS3Provider s3Provider)
    {
        _transactionManager = transactionManager;
        _dateTime = dateTime;
        _logger = logger;
        _mediaAssetRepository = mediaAssetRepository;
        _s3Provider = s3Provider;
    }

    public async Task<Result<InitiateUploadResponse, Error>> HandleAsync(
    InitiateUploadCommand command,
    CancellationToken cancellationToken)
    {
        // Создание и валидация VO
        var fileNameResult = FileName.Create(command.FileName);
        if (fileNameResult.IsFailure)
            return fileNameResult.Error;

        var contentTypeResult = ContentType.Create(command.ContentType);
        if (contentTypeResult.IsFailure)
            return contentTypeResult.Error;

        var fileSizeResult = FileSize.Create(command.FileSize);
        if (fileSizeResult.IsFailure)
            return fileSizeResult.Error;

        // Создание метаданных
        var mediaDataResult = MediaData.Create(
            fileNameResult.Value,
            contentTypeResult.Value,
            fileSizeResult.Value,
            expectedChunksCount: 1);
        if (mediaDataResult.IsFailure)
            return mediaDataResult.Error;

        // Создание владельца
        var mediaOwnerResult = MediaOwner.Create(command.Context, command.EntityId);
        if (mediaOwnerResult.IsFailure)
            return mediaOwnerResult.Error;
        
        var assetType = AssetTypeExtensions.ToAssetType(command.AssetType);
        
        string prefix = command.Context.ToLower();
        string bucket = assetType.ToBucketName();

        var storageKeyResult = StorageKey.CreateNew(bucket, prefix);
        if (storageKeyResult.IsFailure)
            return storageKeyResult.Error;

        // Создание ассета
        var mediaAssetResult = MediaAsset.CreateForUpload(
            assetType,
            mediaDataResult.Value,
            mediaOwnerResult.Value,
            storageKeyResult.Value);
        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error;
        
        var uploadUrlResult = await _s3Provider.GenerateUploadUrlAsync(
            storageKeyResult.Value,
            contentTypeResult.Value);
        if (uploadUrlResult.IsFailure)
            return uploadUrlResult.Error;
        
        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        using var transactionScope = transactionResult.Value;

        _mediaAssetRepository.Add(mediaAssetResult.Value);
        mediaAssetResult.Value.BeginUpload();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        var commitResult = transactionScope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;

        var expiresWhen = _dateTime.UtcNow.AddHours(24);

        return new InitiateUploadResponse(
            mediaAssetResult.Value.Id,
            uploadUrlResult.Value,
            expiresWhen,
            RequiredHeaders: 1);
    }
}