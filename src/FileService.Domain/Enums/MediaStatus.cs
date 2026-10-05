namespace FileService.Domain.Enums;

/// <summary>
/// Статус медиа(жизненный цикл)
/// </summary>
public enum MediaStatus
{
    /// <summary>
    /// Ассет создан, загрузка ещё не начата.
    /// </summary>
    PENDING,
    /// <summary>
    /// Загрузка выполняется.
    /// </summary>
    UPLOADING,
    /// <summary>
    /// Загрузка отменена.
    /// </summary>
    CANCELLED,
    /// <summary>
    /// Загрузка завершена.
    /// </summary>
    UPLOADED,
    /// <summary>
    /// Ассет логически удалён.
    /// </summary>
    DELETED,
}