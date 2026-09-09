using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects;
using Shared.Result;

namespace FileService.Domain;

/// <summary>
/// Метаданные медиафайла.
/// </summary>
public class MediaData
{
    /// <summary>
    /// Исходное имя файла.
    /// </summary>
    public FileName FileName { get; }
    
    /// <summary>
    /// Тип ресурса (изображение, документ, видео, превью и т.д.)
    /// </summary>
    public ContentType ContentType { get; }
    
    /// <summary>
    /// Размер файла в байтах.
    /// </summary>
    public FileSize FileSize { get; }
    
    /// <summary>
    /// Ожидаемое количество кусков.
    /// </summary>
    public long ExpectedChunksCount { get; }
    
    // EF Core
    private MediaData() { }

    private MediaData(
        FileName fileName,
        ContentType contentType,
        FileSize fileSize,
        long expectedChunksCount)
    {
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        ExpectedChunksCount = expectedChunksCount;
    }

    public static Result<MediaData, Error> Create(
        FileName fileName,
        ContentType contentType,
        FileSize fileSize,
        long expectedChunksCount)
    {
        if (expectedChunksCount <= 0)
            return GeneralErrors.ValueIsInvalid(
                nameof(expectedChunksCount),
                "Ожидаемое количество кусков не может быть отрицательным.");

        return new MediaData(fileName, contentType, fileSize, expectedChunksCount);
    }
}