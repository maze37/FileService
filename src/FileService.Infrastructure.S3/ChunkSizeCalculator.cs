using CSharpFunctionalExtensions;
using FileService.Core.Abstractions;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace FileService.Infrastructure.S3;

public class ChunkSizeCalculator : IChunkSizeCalculator
{
    private readonly FileStorageOptions _fileStorageOptions;
    
    public ChunkSizeCalculator(IOptions<FileStorageOptions> s3Options)
    {
        _fileStorageOptions = s3Options.Value;
    }
    
    public Result<(int ChunkSize, int TotalChunks), Error> CalculateChunkSize(long fileSize)
    {
        if (_fileStorageOptions.RecommendedChunkSizeBytes <= 0 || _fileStorageOptions.MaxChunks <= 0)
            return GeneralErrors.ValueIsInvalid("Неправильные данные настройки чанков.");

        if (fileSize <= _fileStorageOptions.RecommendedChunkSizeBytes)
            return ((int)fileSize, 1);

        int calculatedChunks = (int)Math.Ceiling((double)fileSize / _fileStorageOptions.RecommendedChunkSizeBytes);

        int actualChunks = Math.Min(calculatedChunks, _fileStorageOptions.MaxChunks);

        long chunkSize = (fileSize + actualChunks - 1) / actualChunks;

        return ((int)chunkSize, actualChunks);
    }
}