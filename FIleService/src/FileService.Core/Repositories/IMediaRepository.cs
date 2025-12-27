using CSharpFunctionalExtensions;
using FileService.Domain.Entities;
using Shared.SharedKernel.Errors;

namespace FileService.Core.Repositories;

public interface IMediaRepository
{
    Task<Result<MediaAsset, Error>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    
    Task<Result<IReadOnlyList<MediaAsset>, Error>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    
    Task<UnitResult<Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);

    Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken);
}