using Core.Abstractions;

namespace FileService.Infrastructure.Postgres;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}