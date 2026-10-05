using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Domain.ValueObjects;

/// <summary>
/// Имя файла с расширением, проверенное при создании.
/// </summary>
public sealed class FileName : ValueObject
{
    /// <summary>
    /// Предельная длина строкового значения в символах.
    /// </summary>
    public const int MAX_LENGTH = 255;

    /// <summary>
    /// Имя файла вместе с расширением.
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// Имя файла без последнего расширения.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Расширение без точки в нижнем регистре.
    /// </summary>
    public string Extension { get; }

    // EF Core
    /// <summary>
    /// Конструктор для восстановления объекта средствами EF Core.
    /// </summary>
    private FileName() { }

    /// <summary>
    /// Инициализирует объект переданными значениями без дополнительных проверок.
    /// </summary>
    private FileName(string name, string extension)
    {
        Name = name;
        Extension = extension;
        Value = $"{Name}.{Extension}";
    }

    /// <summary>
    /// Проверяет имя и длину, выделяет расширение по последней точке и приводит его к нижнему регистру.
    /// </summary>
    public static Result<FileName, Error> Create(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return GeneralErrors.ValueIsInvalid(nameof(fileName));
        
        if (fileName.Length > MAX_LENGTH)
                return GeneralErrors.ValueIsInvalid(nameof(fileName));
        
        int lastDot = fileName.LastIndexOf('.');
        if (lastDot == -1 || lastDot == fileName.Length - 1)
        {
            return GeneralErrors.ValueIsInvalid(nameof(fileName));
        }

        string namePart = fileName[..lastDot];
        string extension = fileName[(lastDot + 1)..].ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(namePart))
            return GeneralErrors.ValueIsInvalid(null, "Имя файла не может состоять только из расширения.");

        return new FileName(namePart, extension);
    }

    /// <summary>
    /// Возвращает компоненты, по которым сравниваются значения объекта.
    /// </summary>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Extension;
        yield return Value;
    }

    /// <summary>
    /// Возвращает строковое представление при неявном преобразовании.
    /// </summary>
    public static implicit operator string(FileName fileName) => fileName.Value;
}