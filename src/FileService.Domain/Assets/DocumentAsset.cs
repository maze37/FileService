using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using SharedKernel;

namespace FileService.Domain.Assets;

/// <summary>
/// Документ (методичка, прикреплённый файл, PDF-конспект и т.д.)
/// </summary>
public sealed class DocumentAsset : MediaAsset
{
    /// <summary>
    /// Имя бакета для хранения файлов этого типа.
    /// </summary>
    public const string BUCKET = "documents";

    /// <summary>
    /// Допустимые расширения файлов без точки.
    /// </summary>
    public static readonly string[] AllowedExtensions = ["pdf", "doc", "docx", "ppt", "pptx"];
    /// <summary>
    /// Максимальный размер файла в байтах.
    /// </summary>
    public const long MAX_BYTES = 50L * 1024 * 1024;

    // EF Core
    /// <summary>
    /// Конструктор для восстановления объекта средствами EF Core.
    /// </summary>
    private DocumentAsset() { }

    /// <summary>
    /// Инициализирует объект переданными значениями без дополнительных проверок.
    /// </summary>
    private DocumentAsset(
        Guid id, 
        MediaData mediaData, 
        MediaOwner owner,
        StorageKey storageKey,
        MediaStatus status)
        : base(id, mediaData, owner, storageKey, status, AssetType.DOCUMENT) { }

    /// <summary>
    /// Проверяет метаданные и создаёт новый ассет в состоянии PENDING.
    /// </summary>
    public static Result<DocumentAsset, Error> Create(MediaData mediaData, MediaOwner owner, StorageKey storageKey)
    {
        var validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        return new DocumentAsset(Guid.CreateVersion7(), mediaData, owner, storageKey, MediaStatus.PENDING);
    }

    /// <summary>
    /// Проверяет расширение, категорию содержимого и допустимый размер файла.
    /// </summary>
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