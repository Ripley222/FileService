using Amazon.S3;
using FileService.Domain.Shared;
using Shared.SharedKernel.Errors;

namespace FileService.Infrastructure.S3.Errors;

public static class S3ErrorMapper
{
    public static Error ToError(Exception ex) => ex switch
    {
        AmazonS3Exception { ErrorCode: "NoSuchBucket" }
            => FileErrors.BucketNotFound(),
        
        AmazonS3Exception { ErrorCode: "NoSuchKey" }
            => FileErrors.ObjectNotFound(),
        
        AmazonS3Exception { ErrorCode: "InvalidObjectState" }
            => FileErrors.InvalidObject(),
        
        AmazonS3Exception { ErrorCode: "AccessDenied" }
            => FileErrors.AccessDenied(),
        
        _ => FileErrors.Unknown()
    };
}