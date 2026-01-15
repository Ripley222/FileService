using CSharpFunctionalExtensions;
using FileService.Contracts.DTOs;
using FileService.Core.DTOs;
using FileService.Domain.ValueObjects;
using Shared.SharedKernel.Errors;

namespace FileService.Core.FileProviders;

public interface IS3Provider
{
    Task<UnitResult<Error>> UploadFileAsync(
        StorageKey storageKey,
        Stream stream,
        MediaData mediaData,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> DownloadFileAsync(
        StorageKey storageKey,
        string tempPath,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> RemoveFileAsync(
        StorageKey storageKey,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> GenerateUploadUrlAsync(
        StorageKey storageKey,
        MediaData mediaData,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey storageKey);

    Task<Result<IReadOnlyList<MediaUrlDto>, Error>> GenerateDownloadUrlsAsync(IReadOnlyList<StorageKey> storageKeys);

    Task<Result<string, Error>> StartMultipartUploadAsync(
        StorageKey storageKey,
        MediaData mediaData,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> CreateChunkUploadUrlAsync(
        StorageKey storageKey,
        string uploadId,
        int partNumber,
        CancellationToken cancellationToken);

    Task<Result<string[], Error>> GenerateAllChunkUploadUrlsAsync(
        StorageKey storageKey,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> CompleteMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> partMyETags,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> AbortMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        CancellationToken cancellationToken);

    Task<Result<string, Error>> ListMultipartUploadAsync(
        string bucketName,
        CancellationToken cancellationToken);
}