using SharedKernel;

namespace FileService.Domain.Errors;

/// <summary>
/// Ошибки операций с файловым хранилищем.
/// </summary>
public static class FileErrors
{
    /// <summary>
    /// Возвращает ошибку отсутствующего бакета.
    /// </summary>
    public static Error BucketNotFound()
    {
        return Error.NotFound("no.such.bucket", "Бакет не найден");
    }

    /// <summary>
    /// Возвращает ошибку отсутствующей сессии загрузки.
    /// </summary>
    public static Error UploadNotFound()
    {
        return Error.NotFound("upload.not.found", "Сессия загрузки не найдена");
    }

    /// <summary>
    /// Возвращает ошибку отсутствующего объекта.
    /// </summary>
    public static Error ObjectNotFound()
    {
        return Error.NotFound("object.not.found", "Объект не найден");
    }

    /// <summary>
    /// Возвращает ошибку отказа в доступе к операции.
    /// </summary>
    public static Error Forbidden()
    {
        return Error.Failure("access.denied", "Недостаточно прав для выполнения операции");
    }

    /// <summary>
    /// Возвращает ошибку некорректных входных данных.
    /// </summary>
    public static Error ValidationFailed()
    {
        return Error.Validation("validation.failed", "Запрос содержит некорректные данные");
    }

    /// <summary>
    /// Возвращает внутреннюю ошибку файлового хранилища.
    /// </summary>
    public static Error InternalServerError()
    {
        return Error.Failure("internal.server.error", "Внутренняя ошибка хранилища");
    }

    /// <summary>
    /// Возвращает ошибку отменённой операции.
    /// </summary>
    public static Error OperationCanceled()
    {
        return Error.Failure("operation.canceled", "Операция была отменена");
    }

    /// <summary>
    /// Возвращает ошибку сетевого взаимодействия с хранилищем.
    /// </summary>
    public static Error NetworkIssue()
    {
        return Error.Failure("network.issue", "Сетевая ошибка при взаимодействии с файловым хранилищем");
    }

    /// <summary>
    /// Возвращает ошибку нераспознанного сбоя хранилища.
    /// </summary>
    public static Error Unknown()
    {
        return Error.Failure("unknown.error", "Произошла неизвестная оценка");
    }
}