using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using SharedKernel;

namespace FileService.Core.HttpCommunication;

public static class HttpResponseMessageExtensions
{
    public static async Task<Result<TResponse, Error>> HandleResponseAsync<TResponse>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
        where TResponse : class
    {
        try
        {
            Envelope<TResponse>? startMultipartResponse = await response.Content
                .ReadFromJsonAsync<Envelope<TResponse>>(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return startMultipartResponse?.Error ?? GeneralErrors.Failure("Error while reading response");
            }

            if (startMultipartResponse is null)
            {
                return GeneralErrors.Failure("Error while reading response");
            }

            if (startMultipartResponse.Error is not null)
            {
                return startMultipartResponse.Error;
            }

            if (startMultipartResponse.Result is null)
            {
                return GeneralErrors.Failure("Error while reading response");
            }

            return startMultipartResponse.Result;
        }
        catch (Exception)
        {
            return GeneralErrors.Failure("Error while reading response");
        }
    }
    
    public static async Task<UnitResult<Error>> HandleResponseAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startMultipartResponse = await response.Content
                .ReadFromJsonAsync<Envelope>(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return startMultipartResponse?.Error ?? GeneralErrors.Failure("Error while reading response");
            }

            if (startMultipartResponse is null)
            {
                return GeneralErrors.Failure("Error while reading response");
            }

            if (startMultipartResponse.Error is not null)
            {
                return startMultipartResponse.Error;
            }

            return UnitResult.Success<Error>();
        }
        catch (Exception)
        {
            return GeneralErrors.Failure("Error while reading response");
        }
    }
}