using FileService.Core.Database;
using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileService.Infrastructure.Postgres;

public class FileServiceDbContext : DbContext, IReadDbContext
{
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<VideoAsset> VideoAssets => Set<VideoAsset>();
    public DbSet<PreviewAsset> PreviewAssets => Set<PreviewAsset>();

    public FileServiceDbContext(DbContextOptions<FileServiceDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileServiceDbContext).Assembly);
    }

    public IQueryable<MediaAsset> MediaAssetsQuery => MediaAssets.AsQueryable().AsNoTracking();
}