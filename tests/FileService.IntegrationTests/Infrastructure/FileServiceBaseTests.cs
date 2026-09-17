namespace FileService.IntegrationTests.Infrastructure;

public class FileServiceBaseTests : IClassFixture<IntegrationTestsWebFactory>
{
    public HttpClient AppHttpClient { get; init; }
    public HttpClient HttpClient { get; init; }
    public IServiceProvider Services { get; init; }
    
    public FileServiceBaseTests(IntegrationTestsWebFactory factory)
    {
        AppHttpClient = factory.CreateClient();
        HttpClient = new HttpClient();
        Services = factory.Services;
    }
}