using CSharpFunctionalExtensions;
using Shared.Result;

namespace FileService.Domain.ValueObjects;

public sealed class StorageMetadata
{
    public string ETag { get; }
    public string ActualContentType { get; }
    public long ActualSizeBytes { get; }

    // EF Core
    private StorageMetadata() { }

    private StorageMetadata(string eTag, string actualContentType, long actualSizeBytes)
    {
        ETag = eTag;
        ActualContentType = actualContentType;
        ActualSizeBytes = actualSizeBytes;
    }

    public static Result<StorageMetadata, Error> Create(string eTag, string actualContentType, long actualSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(eTag))
            return GeneralErrors.ValueIsRequired(nameof(eTag));

        if (string.IsNullOrWhiteSpace(actualContentType))
            return GeneralErrors.ValueIsRequired(nameof(actualContentType));

        if (actualSizeBytes <= 0)
            return GeneralErrors.ValueIsInvalid(nameof(actualSizeBytes), "Размер файла должен быть положительным");

        return new StorageMetadata(eTag, actualContentType, actualSizeBytes);
    }
}