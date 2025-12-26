using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Core.FileProviders;
using FileService.Domain.ValueObjects;
using FileService.Infrastructure.S3.Errors;
using FileService.Infrastructure.S3.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.SharedKernel.Errors;

namespace FileService.Infrastructure.S3.FIleProviders;

public class S3Provider : IS3Provider
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3Options;
    private readonly ILogger<S3Provider> _logger;

    public S3Provider(IAmazonS3 s3Client, IOptions<S3Options> options, ILogger<S3Provider> logger)
    {
        _s3Client = s3Client;
        _s3Options = options.Value;
        _logger = logger;
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

    public async Task<Result<IReadOnlyList<string>, Error>> GenerateDownloadUrlsAsync(
        IEnumerable<StorageKey> storageKeys)
    {
        try
        {
            List<string> presignedUrlsList = [];
            foreach (var storageKey in storageKeys)
            {
                var getPresignedUrlRequest = new GetPreSignedUrlRequest
                {
                    BucketName = storageKey.Bucket,
                    Key = storageKey.Key,
                    Expires = DateTime.UtcNow.AddHours(_s3Options.DownloadUrlExpirationHours),
                    Verb = HttpVerb.GET,
                };

                var presignedUrl = await _s3Client.GetPreSignedURLAsync(getPresignedUrlRequest);
                presignedUrlsList.Add(presignedUrl);
            }

            return presignedUrlsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download S3 urls.");
            return S3ErrorMapper.ToError(ex);
        }
    }
}