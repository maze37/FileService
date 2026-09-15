using FileService.Domain.Assets;

namespace FileService.Domain.Enums;

/// <summary>
/// Назначение файла — определяет конкретный подтип MediaAsset и bucket в S3.
/// </summary>
public enum AssetType
{
    VIDEO,
    AUDIO,
    DOCUMENT,
    AVATAR,
    COVER,
    PREVIEW,
    THUMBNAIL
}

public static class AssetTypeExtensions
{
    public static string ToBucketName(this AssetType assetType) => assetType switch
    {
        AssetType.VIDEO => VideoAsset.BUCKET,
        AssetType.AUDIO => AudioAsset.BUCKET,
        AssetType.DOCUMENT => DocumentAsset.BUCKET,
        AssetType.AVATAR or AssetType.COVER or AssetType.PREVIEW or AssetType.THUMBNAIL => PreviewAsset.BUCKET,
        _ => throw new ArgumentOutOfRangeException(nameof(assetType), assetType, "Неизвестный AssetType")
    };

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