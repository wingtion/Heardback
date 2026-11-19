using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heardback.IntegrationTests.Applications;

namespace Heardback.IntegrationTests.Matching;

/// <summary>Test-side view of the tailoring and AI-consent endpoints. DTOs pin the wire format.</summary>
internal static class TailoringApi
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> GetTailoringAsync(this HttpClient client, string? accessToken, Guid id) =>
        client.SendAsync(HttpMethod.Get, $"{ApplicationsApi.BasePath}/{id}/tailoring", accessToken);

    public static Task<HttpResponseMessage> RequestTailoringAsync(this HttpClient client, string? accessToken, Guid id) =>
        client.SendAsync(HttpMethod.Post, $"{ApplicationsApi.BasePath}/{id}/tailoring", accessToken);

    public static async Task<TailoringDto> GetTailoringOkAsync(this HttpClient client, string accessToken, Guid id)
    {
        using var response = await client.GetTailoringAsync(accessToken, id);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<TailoringDto>();
    }

    public static async Task<TailoringDto> RequestTailoringOkAsync(this HttpClient client, string accessToken, Guid id)
    {
        using var response = await client.RequestTailoringAsync(accessToken, id);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<TailoringDto>();
    }

    public static async Task<AiConsentDto> GetAiConsentAsync(this HttpClient client, string accessToken)
    {
        using var response = await client.SendAsync(HttpMethod.Get, $"{CvApi.BasePath}/ai-consent", accessToken);
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<AiConsentDto>();
    }

    public static async Task<AiConsentDto> SetAiConsentAsync(this HttpClient client, string accessToken, bool allowed)
    {
        using var response = await client.SendAsync(
            HttpMethod.Put, $"{CvApi.BasePath}/ai-consent", accessToken, JsonContent.Create(new { allowed }));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<AiConsentDto>();
    }

    private static async Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        HttpMethod method,
        string path,
        string? accessToken,
        HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative)) { Content = content };
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }
}

internal sealed record TailoringDto(string State, TailoringDetailsDto? Tailoring, string[] MissingKeywords, UsageDto Usage);

internal sealed record TailoringDetailsDto(
    string Summary,
    TailoringAdviceDto[] Suggestions,
    bool IsOutdated,
    DateTimeOffset CreatedAt);

internal sealed record TailoringAdviceDto(string CvSection, string CvQuote, string Advice);

internal sealed record AiConsentDto(bool Allowed, DateTimeOffset? AllowedAt);
