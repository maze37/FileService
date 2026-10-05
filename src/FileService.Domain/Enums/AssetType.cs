using FileService.Domain.Assets;

namespace FileService.Domain.Enums;

/// <summary>
/// Назначение файла — определяет конкретный подтип MediaAsset и bucket в S3.
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Видео.
    /// </summary>
    VIDEO,
    /// <summary>
    /// Аудио.
    /// </summary>
    AUDIO,
    /// <summary>
    /// Документ.
    /// </summary>
    DOCUMENT,
    /// <summary>
    /// Аватар.
    /// </summary>
    AVATAR,
    /// <summary>
    /// Обложка.
    /// </summary>
    COVER,
    /// <summary>
    /// Изображение предварительного просмотра.
    /// </summary>
    PREVIEW,
    /// <summary>
    /// Уменьшенная миниатюра.
    /// </summary>
    THUMBNAIL
}

/// <summary>
/// Преобразования назначения ассета в имя бакета и из входной строки.
/// </summary>
public static class AssetTypeExtensions
{
    /// <summary>
    /// Возвращает бакет для назначения ассета; неизвестное значение вызывает исключение.
    /// </summary>
    public static string ToBucketName(this AssetType assetType) => assetType switch
    {
        AssetType.VIDEO => VideoAsset.BUCKET,
        AssetType.AUDIO => AudioAsset.BUCKET,
        AssetType.DOCUMENT => DocumentAsset.BUCKET,
        AssetType.AVATAR or AssetType.COVER or AssetType.PREVIEW or AssetType.THUMBNAIL => PreviewAsset.BUCKET,
        _ => throw new ArgumentOutOfRangeException(nameof(assetType), assetType, "Неизвестный AssetType")
    };

    /// <summary>
    /// Преобразует строку в тип ассета; роли изображений объединяет в PREVIEW, неизвестные значения отклоняет.
    /// </summary>
    public static AssetType ToAssetType(this string value)
    {
        return value.ToLowerInvariant() switch
        {
            "video" => AssetType.VIDEO,
            "audio" => AssetType.AUDIO,
            "document" => AssetType.DOCUMENT,
            "avatar" or "cover" or "preview" or "thumbnail" => AssetType.PREVIEW,
            _ => throw new ArgumentException($"invalid asset type: {value}")
        };
    }
}