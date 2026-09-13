using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Shared.Result;

namespace FileService.Domain.Assets;

public static class MediaAssetFactory
{
    public static Result<MediaAsset, Error> Create(
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
}