namespace FileService.Domain.Enums;

/// <summary>
/// Статус медиа(жизненный цикл)
/// </summary>
public enum MediaStatus
{
    PENDING,
    UPLOADING,
    CANCELLED,
    UPLOADED,
    DELETED,
}