using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using Shared.Result;

namespace FileService.Domain.Assets;

/// <summary>
/// Документ (методичка, прикреплённый файл, PDF-конспект и т.д.)
/// </summary>
public sealed class DocumentAsset : MediaAsset
{
    public const string BUCKET = "documents";

    public static readonly string[] AllowedExtensions = ["pdf", "doc", "docx", "ppt", "pptx"];
    public const long MAX_BYTES = 50L * 1024 * 1024;

    // EF Core
    private DocumentAsset() { }

    private DocumentAsset(
        Guid id, 
        MediaData mediaData, 
        MediaOwner owner,
        StorageKey storageKey,
        MediaStatus status)
        : base(id, mediaData, owner, storageKey, status, AssetType.DOCUMENT) { }

    public static Result<DocumentAsset, Error> Create(MediaData mediaData, MediaOwner owner, StorageKey storageKey)
    {
        var validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new DocumentAsset(Guid.CreateVersion7(), mediaData, owner, storageKey, MediaStatus.PENDING);
    }

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
            return Error.Validation("document.invalid.extension",
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}");

        if (mediaData.ContentType.Category != Category.DOCUMENT)
            return Error.Validation("document.invalid.content-type", "File content type must be document");

        if (mediaData.FileSize > MAX_BYTES)
            return Error.Validation("document.invalid.size", $"File size must be less than {MAX_BYTES} bytes");

        return UnitResult.Success<Error>();
    }
}