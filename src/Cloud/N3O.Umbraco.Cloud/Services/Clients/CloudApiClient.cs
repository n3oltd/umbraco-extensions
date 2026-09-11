using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Exceptions;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Json;
using N3O.Umbraco.Validation;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;
using ProblemDetails = N3O.Umbraco.Exceptions.ProblemDetails;

namespace N3O.Umbraco.Cloud;

public class CloudApiClient<TClient> {
    private readonly ILogger<CloudApiClient<TClient>> _logger;
    private readonly TClient _client;
    private readonly IJsonProvider _jsonProvider;

    public CloudApiClient(TClient client, IJsonProvider jsonProvider, ILogger<CloudApiClient<TClient>> logger) {
        _client = client;
        _jsonProvider = jsonProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(Func<TClient, Task> apiCallAsync) {
        try {
            await apiCallAsync(_client);
        } catch (Exception ex) when (IsApiException(ex)) {
            throw ToExceptionWithProblemDetails(ex);
        }
    }
    
    public async Task<TRes> InvokeAsync<TRes>(Func<TClient, Task<TRes>> apiCallAsync) {
        try {
            var res = await apiCallAsync(_client);

            return res;
        } catch (Exception ex) when (IsApiException(ex)) {
            throw ToExceptionWithProblemDetails(ex);
        }
    }
    
    private ExceptionWithProblemDetails ToExceptionWithProblemDetails(Exception exception) {
        try {
            var result = exception.GetType().GetProperty("Result")?.GetValue(exception);
            var statusCode = (int) exception.GetType().GetProperty("StatusCode").GetValue(exception);

            if (statusCode >= 500) {
                _logger.LogError(exception, "Error calling API: {Error}", exception.Message);
            }

            if (result == null) {
                return new CloudApiException(new ProblemDetails((HttpStatusCode) statusCode,
                                                                "Error",
                                                                exception.Message),
                                             exception);
            }

            var content = JsonConvert.SerializeObject(result);

            if (statusCode == StatusCodes.Status412PreconditionFailed ||
                statusCode == StatusCodes.Status422UnprocessableEntity) {
                var validationProblemDetails = _jsonProvider.DeserializeObject<ValidationProblemDetails>(content);

                if (validationProblemDetails?.Errors != null) {
                    return new ValidationException(validationProblemDetails.Errors);
                }
            }

            var problemDetails = JsonConvert.DeserializeObject<ProblemDetails>(content);

            return new CloudApiException(problemDetails, exception);
        } catch (Exception ex) {
            _logger.LogError(ex,
                             $"Error occured converting exception to {nameof(CloudApiException)}: {{Error}}",
                             exception.Message);
            
            throw;
        }
    }

    private bool IsApiException(Exception exception) {
        return exception.GetType().FullName.Contains("ApiException");
    }
}