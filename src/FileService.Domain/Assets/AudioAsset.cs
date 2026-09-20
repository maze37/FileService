using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using SharedKernel;

namespace FileService.Domain.Assets;

/// <summary>
/// Аудио-ресурс (подкасты, озвучка уроков и т.д.)
/// </summary>
public sealed class AudioAsset : MediaAsset
{
    public const string BUCKET = "audio";

    public static readonly string[] AllowedExtensions = ["mp3", "wav", "ogg", "m4a"];
    public const long MAX_BYTES = 100L * 1024 * 1024;

    // EF Core
    private AudioAsset() { }

    private AudioAsset(
        Guid id,
        MediaData mediaData,
        MediaOwner owner,
        StorageKey storageKey,
        MediaStatus status)
        : base(id, mediaData, owner, storageKey, status, AssetType.AUDIO) { }

    public static Result<AudioAsset, Error> Create(MediaData mediaData, MediaOwner owner, StorageKey storageKey)
    {
        var validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new AudioAsset(Guid.CreateVersion7(), mediaData, owner, storageKey, MediaStatus.PENDING);
    }

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
            return Error.Validation("audio.invalid.extension",
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");

        if (mediaData.ContentType.Category != Category.AUDIO)
            return Error.Validation("audio.invalid.content-type", "File content type must be audio");

        if (mediaData.FileSize > MAX_BYTES)
            return Error.Validation("audio.invalid.size", $"File size must be less than {MAX_BYTES} bytes");

        return UnitResult.Success<Error>();
    }
}