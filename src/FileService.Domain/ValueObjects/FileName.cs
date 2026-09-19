using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Domain.ValueObjects;

public sealed class FileName : ValueObject
{
    public const int MAX_LENGTH = 255;

    public string Value { get; }
    
    public string Name { get; }
    
    public string Extension { get; }

    // EF Core
    private FileName() { }

    private FileName(string name, string extension)
    {
        Name = name;
        Extension = extension;
        Value = $"{Name}.{Extension}";
    }

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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Extension;
        yield return Value;
    }

    public static implicit operator string(FileName fileName) => fileName.Value;
}