using CSharpFunctionalExtensions;
using FileService.Domain;
using Shared.Result;

namespace FileService.Core.Abstractions;

public interface IMediaAssetRepository
{
    void Add(MediaAsset asset);

    Task<Result<MediaAsset, Error>> GetByIdAsync(Guid mediaAssetId, CancellationToken cancellationToken);
}
