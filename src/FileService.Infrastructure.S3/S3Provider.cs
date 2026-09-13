using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.Abstractions;
using FileService.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Result;

namespace FileService.Infrastructure.S3;

public class S3Provider : IS3Provider
{
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonS3 _presignClient;
    private readonly S3Options _s3Options;
    private readonly ILogger<S3Provider> _logger;

    public S3Provider(
        IAmazonS3 s3Client,
        [FromKeyedServices(S3ClientKeys.PRESIGN)] IAmazonS3 presignClient,
        IOptions<S3Options> s3Options,
        ILogger<S3Provider> logger)
    {
        _s3Client = s3Client;
        _presignClient = presignClient;
        _s3Options = s3Options.Value;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> UploadFileAsync(
        Stream stream,
        string bucketName,
        StorageKey key,
        string contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key.Value,
                InputStream = stream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object {Bucket}/{Key}", bucketName, key.Value);
            return Error.Failure("s3.upload.failed", $"Не удалось загрузить файл в storage: {ex.Message}");
        }
    }

    public async Task<Result<string, Error>> GenerateDownloadUrlAsync(
        string bucketName,
        StorageKey key,
        CancellationToken cancellationToken)
    {
        var existsResult = await ObjectExistsAsync(bucketName, key, cancellationToken);
        if (existsResult.IsFailure)
            return existsResult.Error;

        if (existsResult.Value is false)
            return Error.NotFound("s3.object.not_found", $"Объект '{key.Value}' не найден в bucket '{bucketName}'");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key.Value,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(_s3Options.DownloadUrlExpirationHours),
            Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP
        };

        try
        {
            string url = await _presignClient.GetPreSignedURLAsync(request);
            return url;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download url for {Bucket}/{Key}", bucketName, key.Value);
            return Error.Failure("s3.download_url.failed", $"Не удалось сгенерировать download url: {ex.Message}");
        }
    }

    public async Task<Result<string, Error>> GenerateUploadUrlAsync(
        string bucketName,
        StorageKey key,
        string contentType,
        CancellationToken cancellationToken)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key.Value,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddHours(_s3Options.UploadUrlExpirationHours),
            Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP
        };

        try
        {
            string url = await _presignClient.GetPreSignedURLAsync(request);
            return url;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to generate upload url for {Bucket}/{Key}", bucketName, key.Value);
            return Error.Failure("s3.upload_url.failed", $"Не удалось сгенерировать upload url: {ex.Message}");
        }
    }

    public async Task<Result<ObjectMetadata, Error>> GetObjectMetadataAsync(
        string bucketName,
        StorageKey key,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = key.Value
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            var metadata = new ObjectMetadata(
                ETag: response.ETag,
                ContentType: response.Headers.ContentType,
                SizeBytes: response.ContentLength,
                LastModified: response.LastModified.GetValueOrDefault());

            return metadata;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Error.NotFound("s3.object.not_found", $"Объект '{key.Value}' не найден в bucket '{bucketName}'");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for {Bucket}/{Key}", bucketName, key.Value);
            return Error.Failure("s3.metadata.failed", $"Не удалось получить metadata: {ex.Message}");
        }
    }

    public async Task<UnitResult<Error>> DeleteObjectAsync(
        string bucketName,
        StorageKey key,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key.Value
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Object {Bucket}/{Key} already absent on delete", bucketName, key.Value);
            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object {Bucket}/{Key}", bucketName, key.Value);
            return Error.Failure("s3.delete.failed", $"Не удалось удалить объект: {ex.Message}");
        }
    }

    public async Task<UnitResult<Error>> EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken)
    {
        try
        {
            bool exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

            if (exists)
            {
                _logger.LogDebug("Bucket {Bucket} already exists", bucketName);
                return UnitResult.Success<Error>();
            }

            var request = new PutBucketRequest
            {
                BucketName = bucketName
            };

            await _s3Client.PutBucketAsync(request, cancellationToken);

            _logger.LogInformation("Bucket {Bucket} created", bucketName);

            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
            _logger.LogDebug("Bucket {Bucket} already exists (race on create)", bucketName);
            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket {Bucket} exists", bucketName);
            return Error.Failure("s3.bucket.init_failed", $"Не удалось создать/проверить bucket '{bucketName}': {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _s3Client.ListBucketsAsync(new ListBucketsRequest(), cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "S3 storage is not available");
            return false;
        }
    }

    private async Task<Result<bool, Error>> ObjectExistsAsync(
        string bucketName,
        StorageKey key,
        CancellationToken cancellationToken)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = bucketName, Key = key.Value },
                cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to check object existence {Bucket}/{Key}", bucketName, key.Value);
            return Error.Failure("s3.object.check_failed", $"Не удалось проверить существование объекта: {ex.Message}");
        }
    }
}