using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Domain.ValueObjects;

/// <summary>
/// Фактические метаданные объекта, полученные из файлового хранилища.
/// </summary>
public sealed class StorageMetadata : ValueObject
{
    /// <summary>
    /// Фактический размер объекта в байтах.
    /// </summary>
    public long SizeBytes { get; }
    /// <summary>
    /// Тип содержимого, возвращённый хранилищем.
    /// </summary>
    public string ContentType { get; }
    /// <summary>
    /// Идентификатор версии содержимого, возвращённый хранилищем.
    /// </summary>
    public string ETag { get; }

    /// <summary>
    /// Инициализирует объект переданными значениями без дополнительных проверок.
    /// </summary>
    private StorageMetadata(long sizeBytes, string contentType, string eTag)
    {
        SizeBytes = sizeBytes;
        ContentType = contentType;
        ETag = eTag;
    }

    /// <summary>
    /// Проверяет положительный размер и непустой тип содержимого, сохраняя переданный ETag.
    /// </summary>
    public static Result<StorageMetadata, Error> Create(long sizeBytes, string contentType, string eTag)
    {
        if (sizeBytes <= 0)
            return Error.Validation("storage.metadata.size.invalid", "Размер объекта должен быть больше нуля");

        if (string.IsNullOrWhiteSpace(contentType))
            return Error.Validation("storage.metadata.content_type.invalid", "Content-Type не может быть пустым");

        return new StorageMetadata(sizeBytes, contentType, eTag);
    }

    /// <summary>
    /// Возвращает компоненты, по которым сравниваются значения объекта.
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return SizeBytes;
        yield return ContentType;
        yield return ETag;
    }
}