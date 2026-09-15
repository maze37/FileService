using CSharpFunctionalExtensions;
using FileService.Core.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Result;

namespace FileService.Infrastructure.S3;

public class ChunkSizeCalculator : IChunkSizeCalculator
{
    private readonly S3Options _s3Options;
    
    public ChunkSizeCalculator(IOptions<S3Options> s3Options)
    {
        _s3Options = s3Options.Value;
    }
    
    public Result<(long ChunkSize, int TotalChunks), Error> CalculateChunkSize(
        long fileSize)
    {
        if (_s3Options.RecommendedChunkSizeBytes <= 0 || _s3Options.MaxChunks <= 0)
            return GeneralErrors.ValueIsInvalid(null, "Настройки чанков.");

        if (fileSize <= _s3Options.RecommendedChunkSizeBytes)
            return (fileSize, 1);

        int calculatedChunks = (int)Math.Ceiling((double)fileSize / _s3Options.RecommendedChunkSizeBytes);

        int actualChunks = Math.Min(calculatedChunks, _s3Options.MaxChunks);

        long chunkSize = (fileSize + actualChunks - 1) / actualChunks;

        return (chunkSize, actualChunks);
    }
}