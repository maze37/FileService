using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Domain.ValueObjects;

public sealed class StorageKey : ValueObject
{
    public const int MAX_LENGTH = 1024;

    /// <summary>
    /// Чистый S3-Guid.NewGuid() ключ
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// Папка в которой лежит медиа.
    /// </summary>
    public string Prefix { get; }
    
    /// <summary>
    /// Prefix/Key.
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// S3-бакет.
    /// </summary>
    public string Bucket { get; }
    
    /// <summary>
    /// Полный путь: Bucket/Prefix/Key
    /// </summary>
    public string FullPath { get; }

    // EF Core
    private StorageKey() { }

    private StorageKey(string bucket, string prefix, string key)
    {
        Bucket = bucket;
        Prefix = prefix;
        Key = key;
        Value = string.IsNullOrEmpty(Prefix) ? Key : $"{Prefix}/{Key}";
        FullPath = $"{Bucket}/{Value}";
    }
    
    public static Result<StorageKey, Error> Create(string bucket, string prefix, string key)
    {
        if (string.IsNullOrWhiteSpace(bucket))
            return GeneralErrors.ValueIsInvalid(nameof(bucket), "Bucket не может быть пустым.");

        if (string.IsNullOrWhiteSpace(key))
            return GeneralErrors.ValueIsInvalid(nameof(key), "Key не может быть пустым.");

        prefix ??= string.Empty;

        if (bucket.Contains("..", StringComparison.Ordinal) ||
            prefix.Contains("..", StringComparison.Ordinal) ||
            key.Contains("..", StringComparison.Ordinal))
            return GeneralErrors.ValueIsInvalid(nameof(key), "Путь содержит недопустимую последовательность '..'.");

        if (bucket.Contains('\\') || prefix.Contains('\\') || key.Contains('\\'))
            return GeneralErrors.ValueIsInvalid(nameof(key), "Путь содержит недопустимый символ '\\'.");

        if (key.StartsWith('/') || prefix.StartsWith('/'))
            return GeneralErrors.ValueIsInvalid(nameof(key), "Путь не может начинаться с '/'.");

        int fullLength = bucket.Length + prefix.Length + key.Length + 2;
        if (fullLength > MAX_LENGTH)
            return GeneralErrors.ValueIsInvalid(nameof(key), $"Итоговый путь превышает {MAX_LENGTH} символов.");

        return new StorageKey(bucket, prefix, key);
    }
    
    public static Result<StorageKey, Error> CreateNew(string bucket, string prefix)
    {
        string key = Guid.NewGuid().ToString("N"); 

        return Create(bucket, prefix, key);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Bucket;
        yield return Value;
    }
}