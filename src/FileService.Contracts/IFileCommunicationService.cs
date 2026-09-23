using CSharpFunctionalExtensions;
using FileService.Contracts.Dtos;
using SharedKernel;

namespace FileService.Contracts.HttpCommunication;

public interface IFileCommunicationService
{ 
    Task<Result<GetFileResponse, Error>> GetMediaAsset(
        GetFileRequest request,
        CancellationToken cancellationToken);

    Task<Result<CheckMediaAssetExistsAndReadyResponse, Error>> CheckMediaAssetExistsAndReady(
        Guid assetId,
        CancellationToken cancellationToken);
}
