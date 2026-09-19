using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Domain.ValueObjects;

public sealed class FileSize : ValueObject
{
    // 5 гб
    public const long MAX_BYTES = 5L * 1024 * 1024 * 1024;

    public long Bytes { get; }
    
    // EF Core
    private FileSize() { }

    private FileSize(long bytes)
    {
        Bytes = bytes;
    }

    public static Result<FileSize, Error> Create(long bytes)
    {
        if (bytes <= 0)
            return GeneralErrors.ValueIsInvalid("size", "Размер файла должен быть больше нуля");

        if (bytes > MAX_BYTES)
            return GeneralErrors.ValueIsInvalid("size", $"Размер файла превышает {MAX_BYTES} байт");

        return new FileSize(bytes);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Bytes;
    }

    public static implicit operator long(FileSize size) => size.Bytes;
}