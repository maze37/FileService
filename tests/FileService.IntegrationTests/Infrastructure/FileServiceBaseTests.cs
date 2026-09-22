using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amazon.S3;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Domain.Assets;
using FileService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace FileService.IntegrationTests.Infrastructure;

[CollectionDefinition(NAME)]
public class FileServiceCollection : ICollectionFixture<IntegrationTestsWebFactory>
{
    public const string NAME = "FileService";
}

[Collection(FileServiceCollection.NAME)]
public abstract class FileServiceBaseTests : IAsyncLifetime
{
    protected const int KB = 1024;
    protected const int MB = 1024 * 1024;

    private readonly IntegrationTestsWebFactory _factory;

    public HttpClient AppHttpClient { get; init; }
    public HttpClient HttpClient { get; init; }
    public IServiceProvider Services { get; init; }

    protected IAmazonS3 S3 => Services.GetRequiredService<IAmazonS3>();

    protected FileServiceBaseTests(IntegrationTestsWebFactory factory)
    {
        _factory = factory;
        AppHttpClient = factory.CreateClient();
        HttpClient = new HttpClient();
        Services = factory.Services;
    }

    // Respawn БД + очистка бакетов ПЕРЕД каждым тестом.
    public Task InitializeAsync() => _factory.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task ExecuteInDb(Func<AppDbContext, Task> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(dbContext);
    }

    /// <summary>
    /// Ассет из БД. Если его нет тест падает
    /// </summary>
    protected async Task<MediaAsset> GetAsset(Guid id)
    {
        MediaAsset? asset = null;

        await ExecuteInDb(async db =>
        {
            asset = await db.MediaAssets.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        });

        Assert.NotNull(asset);
        return asset;
    }

    protected async Task<int> CountAssets()
    {
        int count = 0;
        await ExecuteInDb(async db => count = await db.MediaAssets.CountAsync());
        return count;
    }

    protected async Task<bool> ObjectExists(MediaAsset asset)
    {
        try
        {
            await S3.GetObjectMetadataAsync(asset.StorageKey.Bucket, asset.StorageKey.Value);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    protected async Task<(long Size, string ContentType)> GetObjectInfo(MediaAsset asset)
    {
        var metadata = await S3.GetObjectMetadataAsync(asset.StorageKey.Bucket, asset.StorageKey.Value);
        return (metadata.ContentLength, metadata.Headers.ContentType);
    }

    protected static byte[] RandomBytes(int size)
    {
        byte[] data = new byte[size];
        Random.Shared.NextBytes(data);
        return data;
    }

    protected async Task<InitiateUploadResponse> InitiateUpload(
        byte[] data,
        Guid? entityId = null,
        long? declaredSize = null)
    {
        var request = new InitiateUploadRequest(
            FileName: "test.mp4",
            AssetType: "video",
            ContentType: "video/mp4",
            FileSize: declaredSize ?? data.Length,
            Context: "user",
            EntityId: entityId ?? Guid.NewGuid());

        var response = await AppHttpClient.PostAsJsonAsync("api/files/upload/initiate", request);
        Result<InitiateUploadResponse, Error> result = await response.HandleResponseAsync<InitiateUploadResponse>();

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    /// <summary>
    /// Сырой PUT по presigned URL. Статус проверяет вызывающий тест.
    /// </summary>
    protected async Task<HttpResponseMessage> PutToUrl(string url, byte[] data)
    {
        var content = new ByteArrayContent(data);
        content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        return await HttpClient.PutAsync(url, content);
    }

    protected Task<HttpResponseMessage> CompleteUpload(Guid id) =>
        AppHttpClient.PostAsync($"api/files/upload/{id}/complete", null);

    protected Task<HttpResponseMessage> CancelUpload(Guid id) =>
        AppHttpClient.PatchAsync($"api/files/{id}/cancel", null);

    protected Task<HttpResponseMessage> DeleteFile(Guid id) =>
        AppHttpClient.DeleteAsync($"api/files/{id}");

    protected Task<HttpResponseMessage> GetFile(Guid id) =>
        AppHttpClient.GetAsync($"api/files/{id}");

    /// <summary>
    /// initiate - put  complete. Возвращает id готового файла.
    /// </summary>
    protected async Task<Guid> UploadFile(byte[] data, Guid? entityId = null)
    {
        var init = await InitiateUpload(data, entityId);
        await PutToUrl(init.UploadUrl, data);

        var response = await CompleteUpload(init.AssetId);
        Assert.True(response.IsSuccessStatusCode);

        return init.AssetId;
    }
}