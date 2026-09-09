using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using Shared.Result;

namespace FileService.Domain.Assets;

/// <summary>
/// Превью/обложка/аватар/thumbnail
/// </summary>
public sealed class PreviewAsset : MediaAsset
{
    public const string BUCKET = "previews";

    public static readonly string[] AllowedExtensions = ["jpg", "jpeg", "png", "webp"];
    public const long MAX_BYTES = 10L * 1024 * 1024;

    // EF Core
    private PreviewAsset() { }

    private PreviewAsset(
        Guid id, 
        MediaData mediaData, 
        MediaStatus status,
        AssetType assetType, 
        MediaOwner owner)
        : base(id, mediaData, status, assetType, owner) { }
    
    public static Result<PreviewAsset, Error> Create(
        MediaData mediaData, 
        AssetType assetType, 
        MediaOwner owner)
    {
        if (assetType is not (AssetType.AVATAR or AssetType.COVER or AssetType.PREVIEW or AssetType.THUMBNAIL))
            return Error.Validation("preview.invalid.asset-type", "AssetType должен быть изображением-ролью (avatar/cover/preview/thumbnail)");

        var validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new PreviewAsset(Guid.CreateVersion7(), mediaData, MediaStatus.PENDING, assetType, owner);
    }

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
            return Error.Validation("preview.invalid.extension",
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");

        if (mediaData.ContentType.Category != Category.IMAGE)
            return Error.Validation("preview.invalid.content-type", "File content type must be image");

        if (mediaData.FileSize > MAX_BYTES)
            return Error.Validation("preview.invalid.size", $"File size must be less than {MAX_BYTES} bytes");

        return UnitResult.Success<Error>();
    }
}