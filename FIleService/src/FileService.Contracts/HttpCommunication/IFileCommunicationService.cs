using CSharpFunctionalExtensions;
using FileService.Contracts.Requests;
using FileService.Contracts.Responses;
using Shared.SharedKernel.Errors;

namespace FileService.Contracts.HttpCommunication;

public interface IFileCommunicationService
{
    Task<Result<GetMediaAssetInfoResponse, ErrorList>> GetMediaAsset(
        GetMediaAssetInfoRequest request, CancellationToken cancellationToken);
    
    Task<Result<GetMediaAssetsInfoResponse, ErrorList>> GetMediaAssets(
        GetMediaAssetsInfoRequest request, CancellationToken cancellationToken);

    Task<Result<string, ErrorList>> DownloadFile(
        DownloadPresignedUrlRequest request, CancellationToken cancellationToken);

    Task<Result<string, ErrorList>> UploadFile(
        MultipartUploadRequest request, CancellationToken cancellationToken);
    
    Task<Result<string, ErrorList>> RemoveFile(
        RemoveFileRequest request, CancellationToken cancellationToken);
}