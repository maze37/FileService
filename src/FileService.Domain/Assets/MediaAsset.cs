using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using SharedKernel;

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
    /// Владелец медиафайла.
    /// </summary>
    public MediaOwner MediaOwner { get; protected set; } = null!;
    
    /// <summary>
    /// Данные о расположении медиафайла.
    /// </summary>
    public StorageKey StorageKey { get; protected set; } = null!;

    /// <summary>
    /// Информация о весе и
    /// </summary>
    public StorageMetadata StorageMetadata { get; private set; } = null!;
    
    /// <summary>
    /// Каждая часть мультипарт загрузки имеет одинаковый uploadId
    /// </summary>
    public string? UploadId { get; private set; }
    
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
    
    public static Result<MediaAsset, Error> CreateForUpload(
        AssetType assetType,
        MediaData mediaData,
        MediaOwner owner,
        StorageKey storageKey)
    {
        return assetType switch
        {
            AssetType.VIDEO =>
                VideoAsset.Create(mediaData, owner, storageKey)
                    .Map(asset => (MediaAsset)asset),

            AssetType.AUDIO =>
                AudioAsset.Create(mediaData, owner, storageKey)
                    .Map(asset => (MediaAsset)asset),

            AssetType.DOCUMENT =>
                DocumentAsset.Create(mediaData, owner, storageKey)
                    .Map(asset => (MediaAsset)asset),

            AssetType.AVATAR or AssetType.COVER or AssetType.PREVIEW or AssetType.THUMBNAIL =>
                PreviewAsset.Create(mediaData, owner, storageKey, assetType)
                    .Map(asset => (MediaAsset)asset),

            _ => Error.Validation("asset.type.unsupported", $"Неподдерживаемый тип asset-а: {assetType}")
        };
    }
    
    public UnitResult<Error> AttachUploadId(string uploadId)
    {
        if (Status is not MediaStatus.UPLOADING)
            return Error.Failure("media.asset.invalid_status", "Нельзя привязать uploadId вне процесса загрузки");

        UploadId = uploadId;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> BeginUpload()
    {
        if (Status != MediaStatus.PENDING)
            return Error.Conflict("media.asset.cannot.begin.upload", $"Нельзя начать загрузку: текущий статус {Status}");

        Status = MediaStatus.UPLOADING;
        UpdatedWhen = DateTimeOffset.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkUploaded()
    {
        if (Status != MediaStatus.UPLOADING)
            return Error.Conflict("media.asset.cannot.complete.upload", $"Нельзя завершить загрузку: текущий статус {Status}");
        
        Status = MediaStatus.UPLOADED;
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
    
    public UnitResult<Error> CompleteUpload(StorageMetadata metadata)
    {
        if (Status != MediaStatus.UPLOADING)
            return Error.Conflict("media.asset.cannot.complete", $"Нельзя завершить файл: текущий статус {Status}");

        if (metadata.SizeBytes != MediaData.FileSize)
            return Error.Conflict("media.asset.size.mismatch",
                $"Размер объекта в хранилище ({metadata.SizeBytes}) не совпадает с заявленным ({MediaData.FileSize})");

        if (!string.Equals(metadata.ContentType, MediaData.ContentType.Value, StringComparison.OrdinalIgnoreCase))
            return Error.Conflict("media.asset.content_type.mismatch",
                $"Content-Type объекта в хранилище ({metadata.ContentType}) не совпадает с заявленным ({MediaData.ContentType.Value})");

        StorageMetadata = metadata;
        Status = MediaStatus.UPLOADED;
        return UnitResult.Success<Error>();
    }
}