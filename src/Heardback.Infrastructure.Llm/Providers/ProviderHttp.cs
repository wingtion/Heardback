using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Heardback.Infrastructure.Llm.Providers;

/// <summary>Sends a provider request and sorts the outcome into answered, transient or rejected.</summary>
internal static class ProviderHttp
{
    public const string NetworkError = "network_error";
    public const string Timeout = "timeout";
    public const string HttpStatus = "http_status";
    public const string MalformedResponse = "malformed_response";
    public const string EmptyAnswer = "empty_answer";
    public const string Blocked = "blocked";

    /// <summary>Provider answers are small; anything larger is not a real answer.</summary>
    public const long MaxResponseBytes = 1024 * 1024;

    private const int FlexCapacityExceeded = 498;

    public static async Task<ProviderReply> SendAsync<TResponse>(
        HttpClient httpClient,
        HttpRequestMessage request,
        JsonSerializerOptions jsonOptions,
        Func<TResponse, ProviderReply> readAnswer,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ProviderReply.Transient(NetworkError);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancellation the caller did not ask for.
            return ProviderReply.Transient(Timeout);
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
            {
                return IsTransient(response.StatusCode)
                    ? ProviderReply.Transient(HttpStatus, statusCode, ReadRetryAfter(response, timeProvider))
                    : ProviderReply.Rejected(HttpStatus, statusCode);
            }

            try
            {
                var body = await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions, cancellationToken);
                return body is null ? ProviderReply.Transient(MalformedResponse, statusCode) : readAnswer(body);
            }
            catch (JsonException)
            {
                return ProviderReply.Transient(MalformedResponse, statusCode);
            }
            catch (HttpRequestException)
            {
                // Also raised when the body is larger than MaxResponseBytes.
                return ProviderReply.Transient(NetworkError, statusCode);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return ProviderReply.Transient(Timeout, statusCode);
            }
        }
    }

    internal static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout
        || (int)statusCode == FlexCapacityExceeded
        || (int)statusCode >= 500;

    private static TimeSpan? ReadRetryAfter(HttpResponseMessage response, TimeProvider timeProvider)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var wait = date - timeProvider.GetUtcNow();
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }
}
