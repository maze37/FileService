using System.Net.Http.Json;
using FileService.Contracts;
using FileService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Shared.Result;

namespace FileService.IntegrationTests.Infrastructure;

public class FileServiceBaseTests : IClassFixture<FileServiceTestWebFactory>, IAsyncLifetime
{
    protected IServiceProvider Services { get; }
    protected HttpClient Client { get; }
    protected FakeS3Provider FakeS3 { get; }

    private readonly Func<Task> _resetDatabase;

    protected FileServiceBaseTests(FileServiceTestWebFactory factory)
    {
        Services = factory.Services;
        Client = factory.CreateClient();
        FakeS3 = factory.FakeS3;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();

    protected async Task<T> ExecuteInDb<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await action(dbContext);
    }

    protected async Task<Guid> InitiateUploadViaHttp(
        string fileName = "movie.mp4",
        string contentType = "video/mp4",
        long fileSize = 1000,
        string context = "user",
        Guid? entityId = null,
        string assetType = "video")
    {
        var request = new InitiateUploadRequest(
            fileName, contentType, fileSize, context, entityId ?? Guid.NewGuid(), assetType);

        var response = await Client.PostAsJsonAsync("/api/files/upload/initiate", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<InitiateUploadResponse>>();
        return envelope!.Result!.AssetId;
    }

    protected async Task<HttpResponseMessage> CompleteUploadViaHttp(Guid assetId)
        => await Client.PostAsync($"/api/files/upload/{assetId}/complete", null);

    protected async Task<HttpResponseMessage> CancelUploadViaHttp(Guid fileId)
        => await Client.PatchAsync($"/api/files/{fileId}/cancel", null);

    protected async Task<HttpResponseMessage> DeleteFileViaHttp(Guid fileId)
        => await Client.DeleteAsync($"/api/files/{fileId}");

    protected async Task<HttpResponseMessage> GetFileViaHttp(Guid fileId)
        => await Client.GetAsync($"/api/files/{fileId}");

    protected async Task<HttpResponseMessage> GetFilesByTargetEntityViaHttp(string context, Guid entityId)
        => await Client.GetAsync($"/api/files?context={context}&entityId={entityId}");
}