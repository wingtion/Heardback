using System.Net.Http.Headers;
using System.Net.Http.Json;
using JobTrail.IntegrationTests.Api;

namespace JobTrail.IntegrationTests.Identity;

/// <summary>Test-side view of the Identity HTTP API. DTOs are declared here so tests pin the wire format.</summary>
internal static class IdentityApi
{
    public const string RefreshCookieName = "jobtrail_refresh";
    public const string DefaultPassword = "correct horse battery staple";

    private static readonly Uri RegisterUri = new("/api/auth/register", UriKind.Relative);
    private static readonly Uri LoginUri = new("/api/auth/login", UriKind.Relative);
    private static readonly Uri RefreshUri = new("/api/auth/refresh", UriKind.Relative);
    private static readonly Uri LogoutUri = new("/api/auth/logout", UriKind.Relative);
    private static readonly Uri MeUri = new("/api/me", UriKind.Relative);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> RegisterAsync(
        this HttpClient client,
        string email,
        string inviteCode,
        string password = DefaultPassword,
        string displayName = "Test User") =>
        client.PostAsJsonAsync(RegisterUri, new { email, password, displayName, inviteCode }, CancellationToken);

    public static Task<HttpResponseMessage> LoginAsync(
        this HttpClient client,
        string email,
        string password = DefaultPassword) =>
        client.PostAsJsonAsync(LoginUri, new { email, password }, CancellationToken);

    public static Task<HttpResponseMessage> RefreshAsync(this HttpClient client) =>
        client.PostAsync(RefreshUri, content: null, CancellationToken);

    public static Task<HttpResponseMessage> LogoutAsync(this HttpClient client) =>
        client.PostAsync(LogoutUri, content: null, CancellationToken);

    public static async Task<HttpResponseMessage> GetMeAsync(this HttpClient client, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, MeUri);
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }

    /// <summary>Registers the first admin with the bootstrap code and returns their session.</summary>
    public static async Task<AuthResponseDto> RegisterAdminAsync(this HttpClient client, string email = "admin@example.com")
    {
        using var response = await client.RegisterAsync(email, ApiFactory.BootstrapAdminCode);
        response.EnsureSuccessStatusCode();
        return await response.ReadAuthAsync();
    }

    public static async Task<AuthResponseDto> ReadAuthAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<AuthResponseDto>(CancellationToken)
        ?? throw new InvalidOperationException("Empty auth response.");

    public static string? FindRefreshCookieHeader(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.FirstOrDefault(cookie => cookie.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal))
            : null;
}

internal sealed record AuthResponseDto(string AccessToken, DateTimeOffset AccessTokenExpiresAt, CurrentUserDto User);

internal sealed record CurrentUserDto(Guid Id, string Email, string DisplayName, bool IsAdmin);
