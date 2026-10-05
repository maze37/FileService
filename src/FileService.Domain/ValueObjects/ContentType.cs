using CSharpFunctionalExtensions;
using FileService.Domain.Enums;
using SharedKernel;

namespace FileService.Domain.ValueObjects;

/// <summary>
/// Тип содержимого файла и категория, определённая по его строковому представлению.
/// </summary>
public sealed record ContentType
{
    /// <summary>
    /// Предельная длина строкового значения в символах.
    /// </summary>
    public const int MAX_LENGTH = 255;
    
    // Example: video/mp4
    /// <summary>
    /// Строковый тип содержимого, например video/mp4.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Категория, определённая по строке типа содержимого.
    /// </summary>
    public Category Category { get; }

    // EF Core
    /// <summary>
    /// Конструктор для восстановления объекта средствами EF Core.
    /// </summary>
    private ContentType() { }

    /// <summary>
    /// Инициализирует объект переданными значениями без дополнительных проверок.
    /// </summary>
    private ContentType(string value, Category category)
    {
        Value = value;
        Category = category;
    }

    /// <summary>
    /// Проверяет непустое значение и определяет категорию по подстрокам video, audio, image и document.
    /// </summary>
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
    
    /// <summary>
    /// Возвращает строковое представление при неявном преобразовании.
    /// </summary>
    public static implicit operator string(ContentType value) => value.Value;
}