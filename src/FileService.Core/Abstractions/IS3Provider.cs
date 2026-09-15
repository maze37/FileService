using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Domain.ValueObjects;
using Shared.Result;

namespace FileService.Core.Abstractions;

public interface IS3Provider
{
    Task<Result<string, Error>> StartMultipartUploadAsync(
        StorageKey storageKey,
        string contentType,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunksUploadUrlsAsync(
        StorageKey storageKey,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> CompleteMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> partETags,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> AbortMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> UploadFileAsync(
        Stream stream,
        StorageKey storageKey,
        string contentType,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> GenerateDownloadUrlAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> GenerateUploadUrlAsync(
        StorageKey storageKey,
        string contentType);

    Task<Result<StorageMetadata, Error>> GetObjectMetadataAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> DeleteObjectAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}