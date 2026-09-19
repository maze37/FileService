namespace FileService.Core.Abstractions;

/// <summary>
/// Интерфейс для предоставления даты и времени.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Возвращает текущую дату и время в формате UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}