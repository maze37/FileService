namespace FileService.Contracts.HttpCommunication;

public class FileServiceOptions
{
    public string Url { get; init; } = string.Empty;

    public int UrlExpirationDays { get; init; } = 6;

    public int TimeoutSeconds { get; init; } = 7;
}