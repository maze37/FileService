using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.IntegrationTests.Infrastructure;
using SharedKernel;

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

        HttpResponseMessage response = await AppHttpClient
            .PostAsJsonAsync("api/files/multipart/start", request, cancellationToken);
        
        var startMultipartUploadResponse = await response.Content
            .ReadFromJsonAsync<Envelope<StartMultipartUploadResponse>>(cancellationToken);
        
        Result<StartMultipartUploadResponse, Error> startMultipartUploadResult;
        if (!response.IsSuccessStatusCode)
        {
            startMultipartUploadResult = startMultipartUploadResponse.Error
                                         ?? Error.Failure("test.error", "unknown.error");
        }
        
        response.EnsureSuccessStatusCode();
    }
    
    
}