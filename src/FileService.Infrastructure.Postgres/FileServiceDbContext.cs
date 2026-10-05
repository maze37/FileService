using FileService.Core.Abstractions;
using FileService.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace FileService.Infrastructure.Postgres;

public class FileServiceDbContext : DbContext, IReadDbContext
{
    public FileServiceDbContext(DbContextOptions<FileServiceDbContext> options) : base(options) { }
    
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    
    public IQueryable<MediaAsset> MediaAssetsRead => Set<MediaAsset>().AsQueryable().AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("files");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileServiceDbContext).Assembly);

        modelBuilder.MapWolverineEnvelopeStorage("files");

        base.OnModelCreating(modelBuilder);
    }
}