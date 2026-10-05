namespace FileService.Domain.Enums;

/// <summary>
/// Тип медиа (видео, фото и тд.)
/// </summary>
public enum Category
{
    /// <summary>
    /// Категория не распознана.
    /// </summary>
    UNKNOWN,
    /// <summary>
    /// Видео.
    /// </summary>
    VIDEO,
    /// <summary>
    /// Изображение.
    /// </summary>
    IMAGE,
    /// <summary>
    /// Аудио.
    /// </summary>
    AUDIO,
    /// <summary>
    /// Документ.
    /// </summary>
    DOCUMENT
}