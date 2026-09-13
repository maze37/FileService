using FileService.Domain.Assets;

namespace FileService.Domain.Enums;

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
}