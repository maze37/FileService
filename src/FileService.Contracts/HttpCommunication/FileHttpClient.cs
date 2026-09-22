using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace FileService.Contracts.HttpCommunication;

internal class FileHttpClient : IFileCommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileHttpClient> _logger;
    
    public FileHttpClient(
        HttpClient httpClient,
        ILogger<FileHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<Result<GetFileResponse, Error>> GetMediaAsset(
        GetFileRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"api/files/{request.MediaAssetId}", cancellationToken);
            return await response.HandleResponseAsync<GetFileResponse>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media assets for {MediaAssetIds}", request.MediaAssetId);
            
            return Error.Failure("server.internal", "Failed to request media assets info");
        }
    }
}