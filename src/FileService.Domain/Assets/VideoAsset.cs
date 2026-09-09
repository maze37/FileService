using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Shared.Result;

namespace FileService.Domain.Assets;

/// <summary>
/// Видео-ресурс.
/// </summary>
public class VideoAsset : MediaAsset
{
    public const string BUCKET = "videos";
    public const string RAW_PREFIX = "raw";
    public const string ALLOWED_CONTENT_TYPE = "video";

    public static readonly string[] AllowedExtensions = ["mp4", "mkv", "avi", "mov"];

    // EF Core
    private VideoAsset() { }

    private VideoAsset(
        Guid id,
        MediaData mediaData,
        MediaStatus status,
        MediaOwner owner)
        : base(id, mediaData, status, AssetType.VIDEO, owner) { }
    
    public static Result<VideoAsset, Error> Create(MediaData mediaData, MediaOwner owner)
    {
        var validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new VideoAsset(
            Guid.CreateVersion7(),
            mediaData,
            MediaStatus.PENDING,
            owner);
    }
    
    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
        {
            return Error.Validation("video.invalid.extension",
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");
        }

        if (mediaData.ContentType.Category != Category.VIDEO)
        {
            return Error.Validation("video.invalid.content-type", $"File content type must be {ALLOWED_CONTENT_TYPE}");
        }

        if (mediaData.FileSize > FileSize.MAX_BYTES)
        {
            return Error.Validation("video.invalid.size", $"File size must be less than {FileSize.MAX_BYTES} bytes");
        }
        
        return UnitResult.Success<Error>();
    }
}