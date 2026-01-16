using FileService.Domain.Entities;

namespace FileService.Core.Database;

public interface IReadDbContext
{
    IQueryable<MediaAsset> MediaAssetsQuery { get; }
}