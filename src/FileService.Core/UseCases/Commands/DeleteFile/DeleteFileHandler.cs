using Core.Abstractions;
using Core.Database;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace FileService.Core.UseCases.Commands.DeleteFile;

public class DeleteFileHandler : ICommandHandler<DeleteFileCommand, DeleteFileResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DeleteFileHandler> _logger;
    private readonly IMediaAssetRepository _mediaAssetRepository;
    private readonly IS3Provider _s3Provider;

    public DeleteFileHandler(
        ITransactionManager transactionManager,
        ILogger<DeleteFileHandler> logger,
        IMediaAssetRepository mediaAssetRepository,
        IS3Provider s3Provider)
    {
        _transactionManager = transactionManager;
        _logger = logger;
        _mediaAssetRepository = mediaAssetRepository;
        _s3Provider = s3Provider;
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

        // Открываем транзакцию
        var transactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        using var transactionScope = transactionResult.Value;
        
        var markResult = asset.MarkAsDeleted();
        if (markResult.IsFailure)
            return markResult.Error;
        
        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
            return saveResult.Error;

        var commitResult = transactionScope.Commit();
        if (commitResult.IsFailure)
            return commitResult.Error;
        
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