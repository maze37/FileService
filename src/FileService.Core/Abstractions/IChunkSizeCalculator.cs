using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Core.Abstractions;

public interface IChunkSizeCalculator
{
    Result<(int ChunkSize, int TotalChunks), Error> CalculateChunkSize(long fileSize);
}