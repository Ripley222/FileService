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

    public async Task<Result<string, ErrorList>> DownloadFile(DownloadFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            string requestUri = QueryHelpers.AddQueryString(
                $"file-service/files/{request.FileId}/content", 
                "path", 
                request.Path);
            
            HttpResponseMessage message = await _httpClient.GetAsync(requestUri, cancellationToken);
            return await message.HandleResponseAsync<string>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file with id");
            return Error.Failure("download.media.error", ex.Message).ToErrors();
        }
    }

    public async Task<UnitResult<ErrorList>> UploadFile(UploadFileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            HttpContent httpContent = new StreamContent(request.Stream);
            
            HttpResponseMessage message = await _httpClient.PostAsync("file-service/files", httpContent, cancellationToken);
            return await message.HandleResponseAsync(cancellationToken);
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