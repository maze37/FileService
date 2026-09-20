using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Domain.ValueObjects;

public sealed class StorageMetadata : ValueObject
{
    public long SizeBytes { get; }
    public string ContentType { get; }
    public string ETag { get; }

    private StorageMetadata(long sizeBytes, string contentType, string eTag)
    {
        SizeBytes = sizeBytes;
        ContentType = contentType;
        ETag = eTag;
    }

    public static Result<StorageMetadata, Error> Create(long sizeBytes, string contentType, string eTag)
    {
        if (sizeBytes <= 0)
            return Error.Validation("storage.metadata.size.invalid", "Размер объекта должен быть больше нуля");

        if (string.IsNullOrWhiteSpace(contentType))
            return Error.Validation("storage.metadata.content_type.invalid", "Content-Type не может быть пустым");

        return new StorageMetadata(sizeBytes, contentType, eTag);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return SizeBytes;
        yield return ContentType;
        yield return ETag;
    }
}