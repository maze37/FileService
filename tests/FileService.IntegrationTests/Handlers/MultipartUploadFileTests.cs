using System.Net.Http.Json;
using Amazon.S3;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Domain.Enums;
using FileService.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using CompleteMultipartUploadRequest = FileService.Contracts.Dtos.CompleteMultipartUploadRequest;

namespace FileService.IntegrationTests.Handlers;

public class MultipartUploadFileTests : FileServiceBaseTests
{
    private readonly IntegrationTestsWebFactory _factory;
    
    public MultipartUploadFileTests(IntegrationTestsWebFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task  MultipartUpload_FullCycle_PersistsMediaFile()
    {
        // Arrange
        var cancellationToken = new CancellationTokenSource().Token;

        FileInfo fileInfo = new(Path.Combine(AppContext.BaseDirectory, "Resources", "video.mp4"));
        
        // Act
        StartMultipartUploadResponse startMultipartResponse = await StartMultipartUpload(fileInfo, cancellationToken);

        IReadOnlyList<PartETagDto> partETags = await UploadChunks(fileInfo, startMultipartResponse, cancellationToken);

        var result = await CompleteMultipartUpload(startMultipartResponse, partETags, cancellationToken);
        
        // Assert
        Assert.True(result.IsSuccess);
        
        await ExecuteInDb(async db =>
        {
            var mediaAsset = await db.MediaAssets
                .FirstOrDefaultAsync(m => m.Id == startMultipartResponse.MediaAssetId, cancellationToken);
            
            Assert.Equal(MediaStatus.UPLOADED, mediaAsset?.Status);
            Assert.NotNull(mediaAsset);
            
            IAmazonS3 amazonS3Client = _factory.Services.GetRequiredService<IAmazonS3>();

            var objectResponse = await amazonS3Client.GetObjectAsync(
                mediaAsset.StorageKey.Bucket,
                mediaAsset.StorageKey.Value,
                cancellationToken);

            Assert.Equal(objectResponse.ContentLength, fileInfo.Length);
        });
    }

    private async Task<StartMultipartUploadResponse> StartMultipartUpload(
        FileInfo fileInfo, 
        CancellationToken cancellationToken)
    {
        var request = new StartMultipartUploadRequest(
            fileInfo.Name,
            "video",
            "video/mp4",
            fileInfo.Length,
            "user",
            Guid.Parse("9c1c4802-da4b-4959-8563-175102a3217b"));

        HttpResponseMessage startMultipartResponse = await AppHttpClient
            .PostAsJsonAsync("api/files/multipart/start", request, cancellationToken);

        Result<StartMultipartUploadResponse, Error> startMultipartResult = await startMultipartResponse
            .HandleResponseAsync<StartMultipartUploadResponse>(cancellationToken);
        
        Assert.True(startMultipartResult.IsSuccess);

        await ExecuteInDb(async db =>
        {
            var mediaAsset = await db.MediaAssets
                .FirstOrDefaultAsync(m => m.Id == startMultipartResult.Value.MediaAssetId, cancellationToken);
            
            Assert.Equal(MediaStatus.UPLOADING, mediaAsset?.Status);
            Assert.NotNull(mediaAsset);
        });

        return startMultipartResult.Value;
    }

    private async Task<IReadOnlyList<PartETagDto>> UploadChunks(
        FileInfo fileInfo, 
        StartMultipartUploadResponse startMultipartResponse, 
        CancellationToken cancellationToken)
    {
        await using FileStream stream  = fileInfo.OpenRead();

        var parts = new List<PartETagDto>();

        foreach (ChunkUploadUrl chunkUploadUrl in startMultipartResponse.ChunkUploadUrls.OrderBy(c => c.PartNumber))
        {
            byte[] chunk = new byte[startMultipartResponse.ChunkSize];
            
            int bytesRead = await stream.ReadAsync(chunk, 0, startMultipartResponse.ChunkSize, cancellationToken);
            if (bytesRead == 0)
                break;
            
            var content = new ByteArrayContent(chunk);

            var response = await HttpClient.PutAsync(chunkUploadUrl.UploadUrl, content, cancellationToken);

            var eTag = response.Headers.ETag?.Tag.Trim('"');
            
            parts.Add(new PartETagDto(chunkUploadUrl.PartNumber, eTag!));
        }

        return parts;
    }

    private async Task<UnitResult<Error>> CompleteMultipartUpload(
        StartMultipartUploadResponse startMultipartResponse,
        IReadOnlyList<PartETagDto> partETags,
        CancellationToken cancellationToken)
    {
        var completeRequest = new CompleteMultipartUploadRequest(
            startMultipartResponse.MediaAssetId,
            startMultipartResponse.UploadId,
            partETags);

        HttpResponseMessage completeResponse = await AppHttpClient
            .PostAsJsonAsync("api/files/multipart/complete", completeRequest, cancellationToken);

        UnitResult<Error> completeMultipartResult = await completeResponse
            .HandleResponseAsync(cancellationToken);

        return completeMultipartResult;
    }
}