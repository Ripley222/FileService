using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects;
using Shared.SharedKernel.Errors;

namespace FileService.Core.FileProviders;

public interface IS3Provider
{
    Task<UnitResult<Error>> UploadFileAsync(StorageKey storageKey, Stream stream, MediaData mediaData, CancellationToken cancellationToken);
    
    Task<Result<string, Error>> DownloadFileAsync(StorageKey storageKey, string tempPath, CancellationToken cancellationToken);
    
    Task<Result<string, Error>> RemoveFileAsync(StorageKey storageKey, CancellationToken cancellationToken);
    
    Task<Result<string, Error>> GenerateUploadUrlAsync(StorageKey storageKey, MediaData mediaData, CancellationToken cancellationToken);
    
    Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey storageKey);
    
    Task<Result<IReadOnlyList<string>, Error>> GenerateDownloadUrlsAsync(IEnumerable<StorageKey> storageKeys);
}