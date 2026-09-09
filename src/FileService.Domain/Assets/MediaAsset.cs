using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Shared.Result;

namespace FileService.Domain.Assets;

public abstract class MediaAsset
{
    public const int MAX_LENGTH = 1024;
    
    public Guid Id { get; protected set; }

    /// <summary>
    /// Метаданные о файле.
    /// </summary>
    public MediaData MediaData { get; protected set; } = null!;
    
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
    /// Данные о расположении медиафайла.
    /// </summary>
    public StorageKey StorageKey { get; protected set; } = null!;
    
    /// <summary>
    /// Владелец медиафайла.
    /// </summary>
    public MediaOwner MediaOwner { get; protected set; } = null!;

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
        MediaStatus status,
        AssetType assetType, 
        MediaOwner owner)
    {
        Id = id;
        MediaData = mediaData;
        Status = status;
        AssetType = assetType;
        MediaOwner = owner;
        IsTemporary = false;
        CreatedWhen = DateTimeOffset.UtcNow;
    }

    public UnitResult<Error> BeginUpload()
    {
        if (Status != MediaStatus.PENDING)
            return Error.Conflict("media.asset.cannot.begin.upload", $"Нельзя начать загрузку: текущий статус {Status}");

        Status = MediaStatus.UPLOADING;
        UpdatedWhen = DateTimeOffset.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> CompleteUpload(StorageKey storageKey)
    {
        if (Status != MediaStatus.UPLOADING)
            return Error.Conflict("media.asset.cannot.complete.upload", $"Нельзя завершить загрузку: текущий статус {Status}");

        StorageKey = storageKey;
        Status = MediaStatus.READY;
        UpdatedWhen = DateTimeOffset.UtcNow;
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

    public UnitResult<Error> MakePermanent()
    {
        if (!IsTemporary)
            return Error.Conflict("media.asset.already.permanent", "Файл уже постоянный");

        IsTemporary = false;
        UpdatedWhen = DateTimeOffset.UtcNow;
        return UnitResult.Success<Error>();
    }
}