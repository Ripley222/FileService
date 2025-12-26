using Amazon.S3;
using Amazon.S3.Model;
using FileService.Infrastructure.S3.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3.BackgroundServices;

public class S3BucketsInitializationService : BackgroundService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IOptions<S3Options> _options;
    private readonly ILogger<S3BucketsInitializationService> _logger;

    public S3BucketsInitializationService(
        IAmazonS3 s3Client,
        IOptions<S3Options> options,
        ILogger<S3BucketsInitializationService> logger)
    {
        _s3Client = s3Client;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("S3BucketsInitializationService is starting.");

            if (_options.Value.RequiredBuckets.Count == 0)
            {
                _logger.LogInformation("Buckets are required.");
                throw new ArgumentException("Buckets are required.");
            }

            var tasks = _options.Value.RequiredBuckets
                .Select(bucket => InitializeBucketsAsync(bucket, stoppingToken))
                .ToArray();

            await Task.WhenAll(tasks);

            _logger.LogInformation("Success buckets create.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3BucketsInitializationService is failed stoped.");
            throw;
        }
    }

    private async Task InitializeBucketsAsync(string bucketName, CancellationToken cancellationToken)
    {
        await _s3Client.EnsureBucketExistsAsync(bucketName);

        string policy = $$"""
                       {
                           "Version": "2012-10-17",
                           "Statement":[
                               {
                                "Effect": "Allow",
                                "Principal": {
                                    "AWS": ["*"]
                               },
                               "Action": ["s3:GetObject"],
                               "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
                               }
                           ]
                       }
                       """;

        var putBucketPolicyRequest = new PutBucketPolicyRequest
        {
            BucketName = bucketName,
            Policy = policy
        };

        await _s3Client.PutBucketPolicyAsync(putBucketPolicyRequest, cancellationToken);
    }
}