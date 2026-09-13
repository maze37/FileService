using CSharpFunctionalExtensions;
using Shared.Result;

namespace FileService.Domain;

public sealed record MediaOwner
{
    public const int MAX_LENGTH = 255;
    
    public static readonly HashSet<string> AllowedContexts =
    [
        "lesson",
        "module",
        "user",
        "department",
        "course"
    ];
    
    public string Context { get; }
    
    public Guid EntityId { get; }
    
    // EF Core
    private MediaOwner() { }

    private MediaOwner(string context, Guid entityId)
    {
        Context = context;
        EntityId = entityId;
    }

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
    
    public static Result<MediaOwner, Error> ForLesson(Guid lessonId) => Create("lesson", lessonId);
    public static Result<MediaOwner, Error> ForCourse(Guid courseId) => Create("course", courseId);
    public static Result<MediaOwner, Error> ForUser(Guid userId) => Create("user", userId);
    public static Result<MediaOwner, Error> ForDepartment(Guid departmentId) => Create("department", departmentId);
}