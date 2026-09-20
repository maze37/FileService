using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Domain.Assets;
using SharedKernel;

namespace FileService.Core.Abstractions;

public interface IMediaAssetRepository
{
    void Add(MediaAsset asset);

    Task<Result<MediaAsset, Error>> GetByIdAsync(Guid mediaAssetId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MediaAsset>> GetByOwnerAsync(
        string context,
        Guid entityId,
        CancellationToken cancellationToken);

    Task<Result<MediaAsset, Error>> GetByAsync(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default);

    void Remove(MediaAsset mediaAsset);
}
