using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Contracts.DTOs;
using FileService.Core.DTOs;
using FileService.Core.FileProviders;
using FileService.Domain.ValueObjects;
using FileService.Infrastructure.S3.Errors;
using FileService.Infrastructure.S3.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.SharedKernel.Errors;

namespace FileService.Infrastructure.S3.FIleProviders;

public class S3Provider : IS3Provider, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3Options;
    private readonly ILogger<S3Provider> _logger;

    private readonly SemaphoreSlim _semaphoreSlim;

    public S3Provider(IAmazonS3 s3Client, IOptions<S3Options> options, ILogger<S3Provider> logger)
    {
        _s3Client = s3Client;
        _s3Options = options.Value;
        _logger = logger;
        _semaphoreSlim = new SemaphoreSlim(_s3Options.MaxConcurrentRequests);
    }

    public async Task<UnitResult<Error>> UploadFileAsync(
        StorageKey storageKey, Stream stream, MediaData mediaData, CancellationToken cancellationToken)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Key,
                ContentType = mediaData.ContentType.Value,
                InputStream = stream
            };

            await _s3Client.PutObjectAsync(request, cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading S3 file.");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> DownloadFileAsync(
        StorageKey storageKey, string tempPath, CancellationToken cancellationToken)
    {
        try
        {
            var getObjectRequest = new GetObjectRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Key
            };

            var objectResponse = await _s3Client.GetObjectAsync(getObjectRequest, cancellationToken);

            var pathToFile = tempPath + "\\" + storageKey.Key;
            await objectResponse.WriteResponseStreamToFileAsync(pathToFile, true, cancellationToken);
            return objectResponse.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading S3 file.");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> RemoveFileAsync(StorageKey storageKey, CancellationToken cancellationToken)
    {
        try
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Key
            };

            var deleteObjectResponse = await _s3Client.DeleteObjectAsync(deleteObjectRequest, cancellationToken);
            return deleteObjectResponse.DeleteMarker;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting S3 file.");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateUploadUrlAsync(
        StorageKey storageKey, MediaData mediaData, CancellationToken cancellationToken)
    {
        try
        {
            var createPresignedPostRequest = new CreatePresignedPostRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Key,
                Expires = DateTime.UtcNow.AddMinutes(_s3Options.UploadUrlExpirationMinutes)
            };

            var presignedPostResponse = await _s3Client.CreatePresignedPostAsync(createPresignedPostRequest);
            return presignedPostResponse.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating upload S3 url.");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey storageKey)
    {
        try
        {
            var getPresignedUrlRequest = new GetPreSignedUrlRequest
            {
                BucketName = storageKey.Bucket,
                Key = storageKey.Key,
                Expires = DateTime.UtcNow.AddHours(_s3Options.DownloadUrlExpirationHours),
                Verb = HttpVerb.GET
            };

            var presignedUrl = await _s3Client.GetPreSignedURLAsync(getPresignedUrlRequest);
            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download S3 url.");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<MediaUrlDto>, Error>> GenerateDownloadUrlsAsync(
        IReadOnlyList<StorageKey> storageKeys)
    {
        try
        {
            var tasks = storageKeys.Select(async storageKey =>
            {
                await _semaphoreSlim.WaitAsync();
                
                var getPresignedUrlRequest = new GetPreSignedUrlRequest
                {
                    BucketName = storageKey.Bucket,
                    Key = storageKey.Key,
                    Expires = DateTime.UtcNow.AddHours(_s3Options.DownloadUrlExpirationHours),
                    Verb = HttpVerb.GET,
                };

                var presignedUrl = await _s3Client.GetPreSignedURLAsync(getPresignedUrlRequest);

                return new MediaUrlDto(storageKey, presignedUrl);
            });

            var presignedUrls = await Task.WhenAll(tasks);

            return presignedUrls;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download S3 urls.");
            return S3ErrorMapper.ToError(ex);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<Result<string, Error>> StartMultipartUploadAsync(
        StorageKey storageKey,
        MediaData mediaData,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new InitiateMultipartUploadRequest
            {
                Key = storageKey.Value,
                BucketName = storageKey.Bucket,
                ContentType = mediaData.ContentType.Value
            };

            var response = await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);

            return response.UploadId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting multipart upload.");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> CreateChunkUploadUrlAsync(
        StorageKey storageKey,
        string uploadId,
        int partNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                Verb = HttpVerb.PUT,
                PartNumber = partNumber,
                UploadId = uploadId,
                Expires = DateTime.UtcNow.AddMinutes(_s3Options.UploadUrlExpirationMinutes)
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating S3 chunk upload.");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string[], Error>> GenerateAllChunkUploadUrlsAsync(
        StorageKey storageKey,
        string uploadId,
        int totalChunks,
        CancellationToken cancellationToken)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);

            var startExpiresDateTime = DateTime.UtcNow;
            IEnumerable<Task<string>> tasksList = Enumerable.Range(0, totalChunks)
                .Select(counter =>
                {
                    var request = new GetPreSignedUrlRequest
                    {
                        Key = storageKey.Value,
                        BucketName = storageKey.Bucket,
                        Verb = HttpVerb.PUT,
                        PartNumber = counter,
                        UploadId = uploadId,
                        Expires = startExpiresDateTime.AddMinutes(_s3Options.UploadUrlExpirationMinutes)
                    };
                    var presignedUrlForChunk = _s3Client.GetPreSignedURLAsync(request);

                    return presignedUrlForChunk;
                });

            var chunksUploadUrls = await Task.WhenAll(tasksList);

            return chunksUploadUrls;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating S3 chunk uploads.");
            return S3ErrorMapper.ToError(ex);
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<UnitResult<Error>> CompleteMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        IReadOnlyList<PartETagDto> partMyETags,
        CancellationToken cancellationToken)
    {
        try
        {
            List<PartETag> partETags = [];
            foreach (var eTag in partMyETags)
            {
                partETags.Add(new PartETag(eTag.PartNumber, eTag.ETag));
            }

            var request = new CompleteMultipartUploadRequest
            {
                Key = storageKey.Value,
                BucketName = storageKey.Bucket,
                UploadId = uploadId,
                PartETags = partETags
            };

            await _s3Client.CompleteMultipartUploadAsync(request, cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading S3 file.");
            return UnitResult.Failure(S3ErrorMapper.ToError(ex));
        }
    }

    public async Task<UnitResult<Error>> AbortMultipartUploadAsync(
        StorageKey storageKey,
        string uploadId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AbortMultipartUploadRequest
            {
                Key = storageKey.Value,
                BucketName = storageKey.Bucket,
                UploadId = uploadId
            };

            await _s3Client.AbortMultipartUploadAsync(request, cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aborting multipart upload.");
            return UnitResult.Failure(S3ErrorMapper.ToError(ex));
        }
    }

    public Task<Result<string, Error>> ListMultipartUploadAsync(
        string bucketName,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _semaphoreSlim.Release();
        _semaphoreSlim.Dispose();
    }
}