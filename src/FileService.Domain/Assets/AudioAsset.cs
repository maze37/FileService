namespace FileService.Domain.Assets;

using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Shared.Result;

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
        MediaStatus status,
        MediaOwner owner)
        : base(id, mediaData, status, AssetType.AUDIO, owner) { }

    public static Result<AudioAsset, Error> Create(MediaData mediaData, MediaOwner owner)
    {
        var validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new AudioAsset(Guid.CreateVersion7(), mediaData, MediaStatus.PENDING, owner);
    }

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
            return Error.Validation("audio.invalid.extension",
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");

        if (mediaData.ContentType.Category != Category.AUDIO)
            return Error.Validation("audio.invalid.content-type", "Файл должен быть в формате айxљ«я");

        if (mediaData.FileSize > MAX_BYTES)
            return Error.Validation("audio.invalid.size", $"File size must be less than {MAX_BYTES} bytes");

        return UnitResult.Success<Error>();
    }
}