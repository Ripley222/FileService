using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using Shared.SharedKernel;
using Shared.SharedKernel.Errors;

namespace FileService.Contracts.HttpCommunication;

public static class HttpResponseMessageExtension
{
    public static async Task<Result<TResponse, ErrorList>> HandleResponseAsync<TResponse>(
        this HttpResponseMessage responseMessage, CancellationToken cancellationToken) where TResponse : class
    {
        try
        {
            Envelope<TResponse>? jsonResponse = await responseMessage.Content
                .ReadFromJsonAsync<Envelope<TResponse>>(cancellationToken);
            
            if (responseMessage.IsSuccessStatusCode is false)
            {
                return jsonResponse?.Errors ?? 
                       Error.Failure("error.reading.response", "Error reading response").ToErrors();
            }

            if (jsonResponse is null)
            {
                return Error.Failure("error.reading.response", "Error reading response").ToErrors();
            }

            if (jsonResponse.Errors is not null)
            {
                return jsonResponse.Errors;
            }

            if (jsonResponse.Result is null)
            {
                return Error.Failure("error.reading.response", "Error reading response").ToErrors();
            }
            
            return jsonResponse.Result;
        }
        catch
        {
            return Error.Failure("error.reading.response", "Error reading response").ToErrors();
        }
    }
    
    public static async Task<UnitResult<ErrorList>> HandleResponseAsync(
        this HttpResponseMessage responseMessage, CancellationToken cancellationToken)
    {
        try
        {
            Envelope? jsonResponse = await responseMessage.Content
                .ReadFromJsonAsync<Envelope>(cancellationToken);
            
            if (responseMessage.IsSuccessStatusCode is false)
            {
                return jsonResponse?.Errors ?? 
                       Error.Failure("error.reading.response", "Error reading response").ToErrors();
            }

            if (jsonResponse is null)
            {
                return Error.Failure("error.reading.response", "Error reading response").ToErrors();
            }

            if (jsonResponse.Errors is not null)
            {
                return jsonResponse.Errors;
            }

            if (jsonResponse.Result is null)
            {
                return Error.Failure("error.reading.response", "Error reading response").ToErrors();
            }

            return UnitResult.Success<ErrorList>();
        }
        catch
        {
            return Error.Failure("error.reading.response", "Error reading response").ToErrors();
        }
    }
}