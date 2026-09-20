using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core.Abstractions;
using FileService.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace FileService.Infrastructure.Postgres.Repositories;

public class MediaAssetRepository : IMediaAssetRepository
{
    private readonly AppDbContext _context;

    public MediaAssetRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Add(MediaAsset mediaAsset)
    {
        _context.MediaAssets.Add(mediaAsset);
    }

    public async Task<Result<MediaAsset, Error>> GetByIdAsync(
        Guid mediaAssetId,
        CancellationToken cancellationToken)
    {
        var mediaAsset = await _context.MediaAssets
            .FirstOrDefaultAsync(ma => ma.Id == mediaAssetId, cancellationToken)
            .ConfigureAwait(false);

        if (mediaAsset is null)
            return Error.NotFound("media.asset.not_found", $"Asset с id '{mediaAssetId}' не найден");

        return mediaAsset;
    }

    public async Task<IReadOnlyList<MediaAsset>> GetByOwnerAsync(
        string context,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        return await _context.MediaAssets
            .Where(a => a.MediaOwner.Context == context && a.MediaOwner.EntityId == entityId)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Result<MediaAsset, Error>> GetByAsync(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mediaAsset = await _context.MediaAssets
                .FirstOrDefaultAsync(predicate, cancellationToken);

            if (mediaAsset is null)
                return GeneralErrors.NotFound(null, "mediaAsset");

            return mediaAsset;
        }
        catch (Exception)
        {
            return Error.Failure("mediaAsset.get.failed", "Не удалось получить ассет.");
        }
    }

    public void Remove(MediaAsset mediaAsset)
    {
        _context.MediaAssets.Remove(mediaAsset);
    }
}