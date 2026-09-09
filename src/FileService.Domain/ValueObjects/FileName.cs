using CSharpFunctionalExtensions;
using Shared.Result;

namespace FileService.Domain.ValueObjects;

public sealed class FileName : ValueObject
{
    public const int MAX_LENGTH = 255;

    public string Name { get; }
    
    public string Extension { get; }

    // EF Core
    private FileName() { }

    private FileName(string name, string extension)
    {
        Name = name;
        Extension = extension;
    }

    public static Result<FileName, Error> Create(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return GeneralErrors.ValueIsInvalid(nameof(fileName), "Название файла не может быть пустым.");

        if (fileName.Length > MAX_LENGTH)
            return GeneralErrors.ValueIsInvalid(nameof(fileName), "Название файла слишком длинное.");
        
        int lastDot = fileName.LastIndexOf('.');
        if (lastDot == -1 || lastDot == fileName.Length - 1)
        {
            return GeneralErrors.ValueIsInvalid(null, "Файл должен иметь расширение.");
        }
        
        string extension = fileName[(lastDot + 1)..].ToLowerInvariant();

        return new FileName(fileName, extension);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Extension;
    }

    public static implicit operator string(FileName fileName) => fileName.Name;
}