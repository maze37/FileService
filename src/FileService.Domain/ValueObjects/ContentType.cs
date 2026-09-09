using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using Shared.Result;

namespace FileService.Domain.ValueObjects;

public sealed record ContentType
{
    public const int MAX_LENGTH = 255;
    
    public string Value { get; }

    public Category Category { get; }

    // EF Core
    private ContentType() { }

    private ContentType(string value, Category category)
    {
        Value = value;
        Category = category;
    }

    public static Result<ContentType, Error> Create(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return GeneralErrors.ValueIsInvalid(nameof(contentType), "Тип контента не может быть пустым.");

        Category category = true switch
        {
            _ when contentType.Contains("video", StringComparison.InvariantCultureIgnoreCase) => Category.VIDEO,
            _ when contentType.Contains("audio", StringComparison.InvariantCultureIgnoreCase) => Category.AUDIO,
            _ when contentType.Contains("image", StringComparison.InvariantCultureIgnoreCase) => Category.IMAGE,
            _ when contentType.Contains("document", StringComparison.InvariantCultureIgnoreCase) => Category.DOCUMENT,
            _ => Category.UNKNOWN,
        };

        return new ContentType(contentType, category);
    }
}