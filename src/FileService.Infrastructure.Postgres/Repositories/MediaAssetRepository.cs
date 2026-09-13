using CSharpFunctionalExtensions;
using FileService.Core.Abstractions;
using FileService.Domain;
using Microsoft.EntityFrameworkCore;
using Shared.Result;

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
}