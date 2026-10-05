using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Domain.ValueObjects;

/// <summary>
/// Положительный размер файла в байтах с ограничением максимального значения.
/// </summary>
public sealed class FileSize : ValueObject
{
    // 5 гб
    /// <summary>
    /// Максимальный размер файла в байтах.
    /// </summary>
    public const long MAX_BYTES = 5L * 1024 * 1024 * 1024;

    /// <summary>
    /// Размер файла в байтах.
    /// </summary>
    public long Value { get; }
    
    // EF Core
    /// <summary>
    /// Конструктор для восстановления объекта средствами EF Core.
    /// </summary>
    private FileSize() { }

    /// <summary>
    /// Инициализирует объект переданными значениями без дополнительных проверок.
    /// </summary>
    private FileSize(long bytes)
    {
        Value = bytes;
    }

    /// <summary>
    /// Создаёт размер больше нуля и не больше максимального допустимого значения.
    /// </summary>
    public static Result<FileSize, Error> Create(long bytes)
    {
        if (bytes <= 0)
            return GeneralErrors.ValueIsInvalid("size", "Размер файла должен быть больше нуля");

        if (bytes > MAX_BYTES)
            return GeneralErrors.ValueIsInvalid("size", $"Размер файла превышает {MAX_BYTES} байт");

        return new FileSize(bytes);
    }

    /// <summary>
    /// Возвращает компоненты, по которым сравниваются значения объекта.
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <summary>
    /// Возвращает размер в байтах при неявном преобразовании.
    /// </summary>
    public static implicit operator long(FileSize size) => size.Value;
}