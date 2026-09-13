using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Shared.Result;

namespace FileService.Domain;

public abstract class MediaAsset
{
    public const int MAX_LENGTH = 1024;
    
    public Guid Id { get; protected set; }

    /// <summary>
    /// Метаданные о файле.
    /// </summary>
    public MediaData MediaData { get; protected set; } = null!;
        
    /// <summary>
    /// Владелец медиафайла.
    /// </summary>
    public MediaOwner MediaOwner { get; protected set; } = null!;
    
    /// <summary>
    /// Данные о расположении медиафайла.
    /// </summary>
    public StorageKey StorageKey { get; protected set; } = null!;
    
    /// <summary>
    /// 
    /// </summary>
    public StorageMetadata? StorageMetadata { get; protected set; }
    
    /// <summary>
    /// Назначение ассета.
    /// </summary>
    public AssetType AssetType { get; protected set; }

    /// <summary>
    /// Статус (жизненный цикл)
    /// </summary>
    public MediaStatus Status { get; protected set; }

    /// <summary>
    /// Флаг временности (temporary/permanent)
    /// </summary>
    public bool IsTemporary { get; protected set; }

    /// <summary>
    /// Дата создания.
    /// </summary>
    public DateTimeOffset CreatedWhen { get; protected set; }
    
    /// <summary>
    /// Дата обновления.
    /// </summary>
    public DateTimeOffset? UpdatedWhen { get; protected set; }
    
    // EF Core
    protected MediaAsset() { }

    protected MediaAsset(
        Guid id,
        MediaData mediaData,
        MediaOwner owner,
        StorageKey storageKey,
        MediaStatus status,
        AssetType assetType)
    {
        Id = id;
        MediaData = mediaData;
        MediaOwner = owner;
        StorageKey = storageKey;
        Status = status;
        AssetType = assetType;
        IsTemporary = false;
        CreatedWhen = DateTimeOffset.UtcNow;
    }

    public UnitResult<Error> BeginUpload(DateTimeOffset updatedTime)
    {
        if (Status != MediaStatus.PENDING)
            return Error.Conflict("media.asset.cannot.begin.upload", $"Нельзя начать загрузку: текущий статус {Status}");

        Status = MediaStatus.UPLOADING;
        UpdatedWhen = updatedTime;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> CompleteUpload(StorageMetadata storageMetadata, DateTimeOffset updatedTime)
    {
        if (Status != MediaStatus.UPLOADING)
            return Error.Conflict("media.asset.cannot.complete.upload", $"Нельзя завершить загрузку: текущий статус {Status}");
        
        if (storageMetadata.ActualSizeBytes != MediaData.FileSize.Bytes)
            return Error.Conflict("media.asset.size.mismatch",
                $"Фактический размер файла ({storageMetadata.ActualSizeBytes}) не совпадает с заявленным ({MediaData.FileSize.Bytes})");

        if (!string.Equals(storageMetadata.ActualContentType, MediaData.ContentType.Value, StringComparison.OrdinalIgnoreCase))
            return Error.Conflict("media.asset.content_type.mismatch",
                $"Фактический content-type ({storageMetadata.ActualContentType}) не совпадает с заявленным ({MediaData.ContentType.Value})");

        StorageMetadata = storageMetadata;
        Status = MediaStatus.READY;
        UpdatedWhen = updatedTime;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsError(string errorMessage)
    {
        if (Status is MediaStatus.DELETED or MediaStatus.READY)
            return Error.Conflict("media.asset.cannot.mark.error", $"Нельзя отметить как ошибку: текущий статус {Status}");
        
        Status = MediaStatus.ERROR;
        UpdatedWhen = DateTimeOffset.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkAsDeleted()
    {
        if (Status == MediaStatus.DELETED)
            return Error.Conflict("media.asset.already.deleted", "Файл уже удалён");

        if (Status == MediaStatus.PENDING)
            return Error.Conflict("media.asset.cannot.delete.pending", "Нельзя удалить файл, который никогда не загружался");

        Status = MediaStatus.DELETED;
        UpdatedWhen = DateTimeOffset.UtcNow;
        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> MarkAsCancelled()
    {
        if (Status == MediaStatus.CANCELLED)
            return Error.Conflict("media.asset.already.cancelled", "Файл уже отменен.");

        if (Status == MediaStatus.PENDING)
            return Error.Conflict("media.asset.cannot.cancel.pending", "Нельзя отменить файл, который никогда не загружался");

        Status = MediaStatus.CANCELLED;
        UpdatedWhen = DateTimeOffset.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MakePermanent()
    {
        if (!IsTemporary)
            return Error.Conflict("media.asset.already.permanent", "Файл уже постоянный");

        IsTemporary = false;
        UpdatedWhen = DateTimeOffset.UtcNow;
        return UnitResult.Success<Error>();
    }
}