using CSharpFunctionalExtensions;
using FileService.Domain.Entities;
using Shared.SharedKernel.Errors;

namespace FileService.Core.Repositories;

public interface IMediaRepository
{
    Task<UnitResult<Error>> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken);
}