using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Domain.ValueObjects;
using Shared.Result;

namespace FileService.Core.Abstractions;

public interface IS3Provider
{
    Task<UnitResult<Error>> UploadFileAsync(
        Stream stream,
        string bucketName,
        StorageKey key,
        string contentType,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> GenerateDownloadUrlAsync(
        string bucketName,
        StorageKey key,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> GenerateUploadUrlAsync(
        string bucketName,
        StorageKey key,
        string contentType,
        CancellationToken cancellationToken);

    Task<Result<ObjectMetadata, Error>> GetObjectMetadataAsync(
        string bucketName,
        StorageKey key,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> DeleteObjectAsync(
        string bucketName,
        StorageKey key,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
}