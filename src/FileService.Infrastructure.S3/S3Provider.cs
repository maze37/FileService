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
using SharedKernel;
using AbortMultipartUploadRequest = Amazon.S3.Model.AbortMultipartUploadRequest;
using CompleteMultipartUploadRequest = Amazon.S3.Model.CompleteMultipartUploadRequest;

namespace FileService.Infrastructure.S3;

public class S3Provider : IDisposable, IS3Provider
{
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonS3 _presignClient;
    private readonly S3Options _s3Options;
    private readonly ILogger<S3Provider> _logger;
    private readonly SemaphoreSlim _requestsSemaphore;

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
        _requestsSemaphore = new SemaphoreSlim(_s3Options.MaxConcurrentRequests);
    }

    public async Task<Result<string, Error>> StartMultipartUploadAsync(
        StorageKey storageKey,
        string contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new InitiateMultipartUploadRequest
            {
                BucketName = storageKey.Bucket, 
                Key = storageKey.Value,
                ContentType = contentType
            };
            
            InitiateMultipartUploadResponse result = await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);

            return result.UploadId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting multipart upload.");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<ChunkUploadUrl>, Error>> GenerateAllChunksUploadUrlsAsync(
        StorageKey storageKey,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Task<ChunkUploadUrl>> tasks = Enumerable.Range(1, totalChunks)
                .Select(async partNumber =>
                {
                    await _requestsSemaphore.WaitAsync(cancellationToken);

                    try
                    {
                        var request = new GetPreSignedUrlRequest
                        {
                            BucketName = storageKey.Bucket,
                            Key = storageKey.Value,
                            Verb = HttpVerb.PUT,
                            UploadId = uploadId,
                            PartNumber = partNumber,
                            Expires = DateTime.UtcNow.AddHours(_s3Options.UploadUrlExpirationHours),
                            Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP
                        };

                        string? response = await _presignClient.GetPreSignedURLAsync(request);

                        return new ChunkUploadUrl(partNumber, response);
                    }
                    finally
                    {
                        _requestsSemaphore.Release();
                    }
                });

            ChunkUploadUrl[] results = await Task.WhenAll(tasks);

            return results;
        }
        catch (Exception ex)
        {
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> CompleteMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> partETags,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new CompleteMultipartUploadRequest()
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                UploadId = uploadId,
                PartETags = partETags.Select(p => new PartETag { ETag = p.ETag, PartNumber = p.PartNumber }).ToList()
            };

            var response = await _s3Client.CompleteMultipartUploadAsync(
                request, 
                cancellationToken);

            return response.Key;
        }
        catch (Exception ex)
        {
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<UnitResult<Error>> AbortMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AbortMultipartUploadRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                UploadId = uploadId
            };

            await _s3Client.AbortMultipartUploadAsync(request, cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to abort multipart upload {UploadId} for {Bucket}/{Key}", uploadId, storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<UnitResult<Error>> UploadFileAsync(
        Stream stream,
        StorageKey storageKey,
        string contentType,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                InputStream = stream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to upload object {Bucket}/{Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateDownloadUrlAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var existsResult = await ObjectExistsAsync(storageKey, cancellationToken);
            if (existsResult.IsFailure)
                return existsResult.Error;

            if (existsResult.Value is false)
                return Error.NotFound("s3.object.not_found", $"Объект '{storageKey.Value}' не найден в bucket '{storageKey.Bucket}'");

            var request = new GetPreSignedUrlRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddHours(_s3Options.DownloadUrlExpirationHours),
                Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP
            };
        
            string response = await _presignClient.GetPreSignedURLAsync(request);
            
            return response;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download url for {Bucket}/{Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateUploadUrlAsync(
        StorageKey storageKey,
        string contentType)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
                Verb = HttpVerb.PUT,
                ContentType = contentType,
                Expires = DateTime.UtcNow.AddHours(_s3Options.UploadUrlExpirationHours),
                Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP
            };
            
            string response = await _presignClient.GetPreSignedURLAsync(request);
            
            return response;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to generate upload url for {Bucket}/{Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<StorageMetadata, Error>> GetObjectMetadataAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);

            return StorageMetadata.Create(response.ContentLength, response.Headers.ContentType, response.ETag);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return Error.NotFound("s3.object.not_found", $"Объект '{storageKey.Value}' не найден в bucket '{storageKey.Bucket}'");
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for {Bucket}/{Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<UnitResult<Error>> DeleteObjectAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Value,
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Object {Bucket}/{Key} already absent on delete", storageKey.Bucket, storageKey.Value);
            return UnitResult.Success<Error>();
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to delete object {Bucket}/{Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
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
            return S3ErrorMapper.ToError(ex);
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
        StorageKey storageKey,
        CancellationToken cancellationToken)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(
                new GetObjectMetadataRequest { BucketName = storageKey.Bucket, Key = storageKey.Value },
                cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to check object existence {Bucket}/{Key}", storageKey.Bucket, storageKey.Value);
            return S3ErrorMapper.ToError(ex);
        }
    }

    private string ReplaceEndpoint(string presignedUrl) =>
        presignedUrl.Replace(_s3Options.Endpoint, _s3Options.ExternalEndpoint, StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
        _requestsSemaphore.Dispose();
    }
}