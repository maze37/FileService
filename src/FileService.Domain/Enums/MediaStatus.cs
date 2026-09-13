namespace FileService.Domain.Enums;

/// <summary>
/// Статус медиа(жизненный цикл)
/// </summary>
public enum MediaStatus
{
    PENDING,
    UPLOADING,
    READY,
    ERROR,
    DELETED,
    CANCELLED
}