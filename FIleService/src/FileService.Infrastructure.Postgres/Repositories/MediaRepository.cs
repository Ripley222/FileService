using CSharpFunctionalExtensions;
using FileService.Core.Repositories;
using FileService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel.Errors;

namespace FileService.Infrastructure.Postgres.Repositories;

public class MediaRepository(
    FileServiceDbContext dbContext,
    ILogger<MediaRepository> logger) : IMediaRepository
{
    public async Task<UnitResult<Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);

            return UnitResult.Failure(Error.Failure(
                "database.add.error", $"Ошибка сохранения {nameof(MediaAsset)})"));
        }
    }
}