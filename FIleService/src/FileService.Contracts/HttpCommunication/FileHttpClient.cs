using CSharpFunctionalExtensions;
using FileService.Contracts.Requests;
using FileService.Contracts.Responses;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel.Errors;

namespace FileService.Contracts.HttpCommunication;

internal sealed class FileHttpClient : IFileCommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileHttpClient> _logger;
    
    public FileHttpClient(HttpClient httpClient, ILogger<FileHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<GetMediaAssetInfoResponse, ErrorList>> GetMediaAsset(
        GetMediaAssetInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage message = await _httpClient.GetAsync($"file-service/files/{request.MediaAssetId}", cancellationToken);
            return await message.HandleResponseAsync<GetMediaAssetInfoResponse>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media asset info for {MediaAssetId}", request.MediaAssetId);
            return Error.Failure("get.media.error", ex.Message).ToErrors();
        }
    }

    public async Task<Result<GetMediaAssetsInfoResponse, ErrorList>> GetMediaAssets(
        GetMediaAssetsInfoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage message = await _httpClient.GetAsync($"file-service/files/batch", cancellationToken);
            return await message.HandleResponseAsync<GetMediaAssetsInfoResponse>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media asset info for {MediaAssetIds}", request.MediaAssetIds);
            return Error.Failure("get.media.error", ex.Message).ToErrors();
        }
    }

    public async Task<Result<string, ErrorList>> DownloadFile(DownloadPresignedUrlRequest request, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage message = await _httpClient.GetAsync(
                $"file-service/files/{request.MediaAssetId}/download-url", 
                cancellationToken);
            
            return await message.HandleResponseAsync<string>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file with id");
            return Error.Failure("download.media.error", ex.Message).ToErrors();
        }
    }

    public async Task<Result<string, ErrorList>> UploadFile(MultipartUploadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage message = await _httpClient.GetAsync("files/multipart/start", cancellationToken);
            return await message.HandleResponseAsync<string>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file with id");
            return Error.Failure("remove.media.error", ex.Message).ToErrors();
        }
    }

    public async Task<Result<string, ErrorList>> RemoveFile(
        RemoveFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage message = await _httpClient.DeleteAsync($"file-service/files/{request.MediaAssetId}", cancellationToken);
            return await message.HandleResponseAsync<string>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file with id: {MediaAssetId}", request.MediaAssetId);
            return Error.Failure("remove.media.error", ex.Message).ToErrors();
        }
    }
}