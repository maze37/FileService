using System.Net.Http.Json;
using FileService.Contracts;
using FileService.IntegrationTests.Infrastructure;
using Shared.Result;

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
        var cancellationToken = new CancellationTokenSource().Token;
        
        var request = new StartMultipartUploadRequest(
            "video.mp4",
            "video",
            "video/mp4",
            6479654,
            "user",
            Guid.Parse("9c1c4802-da4b-4959-8563-175102a3217b"));

        var response = await AppHttpClient.PostAsJsonAsync("api/files/multipart/start", request, cancellationToken);

        var data = await response.Content.ReadFromJsonAsync<Envelope<StartMultipartUploadResponse>>(cancellationToken);

        response.EnsureSuccessStatusCode();
        
        Assert.NotNull(data);
        Assert.NotNull(data.Result);
        Assert.NotNull(data.Result.UploadId);
        Assert.NotEmpty(data.Result.ChunkUploadUrls);
    }
}