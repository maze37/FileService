using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Domain;

/// <summary>
/// Ссылка на бизнес-сущность, которой принадлежит медиафайл.
/// </summary>
public sealed record MediaOwner
{
    /// <summary>
    /// Максимальная длина названия контекста владельца.
    /// </summary>
    public const int MAX_LENGTH = 255;
    
    /// <summary>
    /// Поддерживаемые контексты бизнес-сущностей, которым могут принадлежать файлы.
    /// </summary>
    public static readonly HashSet<string> AllowedContexts =
    [
        "location",
        "lesson",
        "module",
        "user",
        "department",
        "course"
    ];
    
    /// <summary>
    /// Контекст владельца в нижнем регистре, например location или user.
    /// </summary>
    public string Context { get; }
    
    /// <summary>
    /// Идентификатор сущности владельца во внешнем сервисе.
    /// </summary>
    public Guid EntityId { get; }
    
    // EF Core
    /// <summary>
    /// Конструктор для восстановления объекта средствами EF Core.
    /// </summary>
    private MediaOwner() { }

    /// <summary>
    /// Инициализирует объект переданными значениями без дополнительных проверок.
    /// </summary>
    private MediaOwner(string context, Guid entityId)
    {
        Context = context;
        EntityId = entityId;
    }

    /// <summary>
    /// Проверяет контекст и идентификатор владельца, нормализуя контекст в нижний регистр.
    /// </summary>
    public static Result<MediaOwner, Error> Create(string context, Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(context) || context.Length > MAX_LENGTH)
            return GeneralErrors.ValueIsInvalid(nameof(context), "Неправильное название контекста.");
        
        string normalizedContext = context.Trim().ToLowerInvariant();
        if (!AllowedContexts.Contains(normalizedContext))
            return GeneralErrors.ValueIsInvalid(nameof(context), "Такого контекста не существует.");
        
        if (entityId == Guid.Empty)
            return GeneralErrors.ValueIsInvalid(nameof(entityId), "EntityId не может быть пустым.");
        
        return new MediaOwner(normalizedContext, entityId);
    }
    
    /// <summary>
    /// Создаёт владельца с контекстом lesson для указанного урока.
    /// </summary>
    public static Result<MediaOwner, Error> ForLesson(Guid lessonId) => Create("lesson", lessonId);
    /// <summary>
    /// Создаёт владельца с контекстом course для указанного курса.
    /// </summary>
    public static Result<MediaOwner, Error> ForCourse(Guid courseId) => Create("course", courseId);
    /// <summary>
    /// Создаёт владельца с контекстом user для указанного пользователя.
    /// </summary>
    public static Result<MediaOwner, Error> ForUser(Guid userId) => Create("user", userId);
    /// <summary>
    /// Создаёт владельца с контекстом department для указанного подразделения.
    /// </summary>
    public static Result<MediaOwner, Error> ForDepartment(Guid departmentId) => Create("department", departmentId);
}