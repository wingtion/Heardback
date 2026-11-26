using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heardback.IntegrationTests.Api;

namespace Heardback.IntegrationTests.Identity;

/// <summary>Test-side view of "Try the demo" (ADR-0018).</summary>
internal static class DemoApi
{
    private static readonly Uri DemoUri = new("/api/demo", UriKind.Relative);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static async Task<HttpResponseMessage> StartDemoAsync(this HttpClient client, string? clientIp = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, DemoUri);
        if (clientIp is not null)
        {
            request.Headers.Add(TestClientIpStartupFilter.HeaderName, clientIp);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<DemoAuthResponseDto> StartDemoOkAsync(this HttpClient client, string? clientIp = null)
    {
        using var response = await client.StartDemoAsync(clientIp);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DemoAuthResponseDto>(CancellationToken)
            ?? throw new InvalidOperationException("Empty demo response.");
    }

    public static async Task<HttpResponseMessage> SendAsAsync(
        this HttpClient client,
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, CancellationToken);
    }
}

internal sealed record DemoAuthResponseDto(string AccessToken, DateTimeOffset AccessTokenExpiresAt, DemoUserDto User);

internal sealed record DemoUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsAdmin,
    bool IsDemo,
    DateTimeOffset? DemoEndsAt);
