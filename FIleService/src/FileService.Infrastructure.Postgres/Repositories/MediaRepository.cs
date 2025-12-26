using CSharpFunctionalExtensions;
using FileService.Core.Repositories;
using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel.Errors;

namespace FileService.Infrastructure.Postgres.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly FileServiceDbContext _fileServiceDbContext;
    private readonly ILogger<MediaRepository> _logger;

    public MediaRepository(
        FileServiceDbContext fileServiceDbContext,
        ILogger<MediaRepository> logger)
    {
        _fileServiceDbContext = fileServiceDbContext;
        _logger = logger;
    }

    public async Task<Result<MediaAsset, Error>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var mediaAsset = await _fileServiceDbContext.MediaAssets
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

            if (mediaAsset is null)
                return Error.NotFound("database.get.error", $"{nameof(MediaAsset)} не найден!");

            return mediaAsset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return Result.Failure<MediaAsset, Error>(
                Error.Failure("database.get.error", $"Ошибка получения {nameof(MediaAsset)})!"));
        }
    }

    public async Task<Result<IReadOnlyList<MediaAsset>, Error>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        try
        {
            var mediaAssets = await _fileServiceDbContext.MediaAssets
                .Where(m => ids.Contains(m.Id))
                .ToListAsync(cancellationToken);

            if (mediaAssets.Count == 0)
                return Error.NotFound("database.get.error", $"{nameof(MediaAsset)} не найден!");

            return mediaAssets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return Result.Failure<IReadOnlyList<MediaAsset>, Error>(
                Error.Failure("database.get.error", $"Ошибка получения {nameof(MediaAsset)})!"));
        }
    }

    public async Task<UnitResult<Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken)
    {
        try
        {
            await _fileServiceDbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken);
            await _fileServiceDbContext.SaveChangesAsync(cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return UnitResult.Failure(Error.Failure(
                "database.add.error", $"Ошибка сохранения {nameof(MediaAsset)})!"));
        }
    }

    public async Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fileServiceDbContext.SaveChangesAsync(cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return UnitResult.Failure(Error.Failure(
                "database.save.error", "Ошибка сохранения данных!"));
        }
    }
}