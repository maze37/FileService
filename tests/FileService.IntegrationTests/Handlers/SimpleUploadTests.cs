using System.Net.Http.Json;
using System.Security.Cryptography;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.HttpCommunication;
using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.IntegrationTests.Infrastructure;
using SharedKernel;

namespace FileService.IntegrationTests.Handlers;

public class SimpleUploadTests : FileServiceBaseTests
{
    public SimpleUploadTests(IntegrationTestsWebFactory factory) : base(factory) { }

    [Theory]
    [InlineData(1)]
    [InlineData(KB)]
    [InlineData(3 * MB)]
    public async Task FullCycle_MetadataInDbAndObjectInStorageAreConsistent(int size)
    {
        byte[] data = RandomBytes(size);
        
        var init = await InitiateUpload(data);
        MediaAsset asset = await GetAsset(init.AssetId);
        Assert.Equal(MediaStatus.UPLOADING, asset.Status);
        Assert.False(await ObjectExists(asset));
        
        await PutToUrl(init.UploadUrl, data);
        
        var completeResponse = await CompleteUpload(init.AssetId);
        Assert.True(completeResponse.IsSuccessStatusCode);
        
        asset = await GetAsset(init.AssetId);
        Assert.Equal(MediaStatus.UPLOADED, asset.Status);

        var (storedSize, storedContentType) = await GetObjectInfo(asset);
        Assert.Equal(size, storedSize);
        Assert.Equal("video/mp4", storedContentType);
        Assert.Equal(SHA256.HashData(data), await GetObjectHash(asset));
        
        var getResponse = await GetFile(init.AssetId);
        Result<GetFileResponse, Error> file = await getResponse.HandleResponseAsync<GetFileResponse>();
        Assert.True(file.IsSuccess);

        byte[] downloaded = await HttpClient.GetByteArrayAsync(file.Value.DownloadUrl);
        Assert.Equal(data, downloaded);
    }

    [Fact]
    public async Task Put_ToTamperedPresignedUrl_IsRejectedByStorage()
    {
        byte[] data = RandomBytes(KB);
        var init = await InitiateUpload(data);
        MediaAsset asset = await GetAsset(init.AssetId);
        
        string tamperedUrl = init.UploadUrl.Replace(asset.StorageKey.Value, asset.StorageKey.Value + "x");
        Assert.NotEqual(init.UploadUrl, tamperedUrl);

        var response = await PutToUrl(tamperedUrl, data);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(await ObjectExists(asset));
    }

    [Fact]
    public async Task Complete_WhenFileWasNotUploaded_FailsAndStatusStaysUploading()
    {
        var init = await InitiateUpload(RandomBytes(KB)); // PUT не делали

        var response = await CompleteUpload(init.AssetId);

        Assert.False(response.IsSuccessStatusCode);
        MediaAsset asset = await GetAsset(init.AssetId);
        Assert.Equal(MediaStatus.UPLOADING, asset.Status);
    }

    [Fact]
    public async Task Complete_Twice_SecondCallFails()
    {
        Guid id = await UploadFile(RandomBytes(KB));

        var second = await CompleteUpload(id);

        Assert.False(second.IsSuccessStatusCode);
        MediaAsset asset = await GetAsset(id);
        Assert.Equal(MediaStatus.UPLOADED, asset.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5L * 1024 * 1024 * 1024 + 1)]
    public async Task Initiate_WithInvalidFileSize_FailsAndNothingIsPersisted(long fileSize)
    {
        var request = new InitiateUploadRequest(
            FileName: "test.mp4",
            AssetType: "video",
            ContentType: "video/mp4",
            FileSize: fileSize,
            Context: "user",
            EntityId: Guid.NewGuid());

        var response = await AppHttpClient.PostAsJsonAsync("api/files/upload/initiate", request);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(0, await CountAssets());
    }
}