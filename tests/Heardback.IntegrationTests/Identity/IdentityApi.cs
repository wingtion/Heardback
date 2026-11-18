using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heardback.IntegrationTests.Api;

namespace Heardback.IntegrationTests.Identity;

/// <summary>Test-side view of the Identity HTTP API. DTOs are declared here so tests pin the wire format.</summary>
internal static class IdentityApi
{
    public const string RefreshCookieName = "heardback_refresh";
    public const string DefaultPassword = "correct horse battery staple";

    private static readonly Uri RegisterUri = new("/api/auth/register", UriKind.Relative);
    private static readonly Uri LoginUri = new("/api/auth/login", UriKind.Relative);
    private static readonly Uri RefreshUri = new("/api/auth/refresh", UriKind.Relative);
    private static readonly Uri LogoutUri = new("/api/auth/logout", UriKind.Relative);
    private static readonly Uri MeUri = new("/api/me", UriKind.Relative);
    private static readonly Uri InviteCodesUri = new("/api/admin/invite-codes", UriKind.Relative);

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
        Authorize(request, accessToken);
        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<HttpResponseMessage> CreateInviteAsync(
        this HttpClient client,
        string? accessToken,
        int? expiresInDays = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, InviteCodesUri)
        {
            Content = expiresInDays is null ? null : JsonContent.Create(new { expiresInDays }),
        };
        Authorize(request, accessToken);
        return await client.SendAsync(request, CancellationToken);
    }

    public static async Task<HttpResponseMessage> ListInvitesAsync(
        this HttpClient client,
        string? accessToken,
        string query = "")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{InviteCodesUri}{query}", UriKind.Relative));
        Authorize(request, accessToken);
        return await client.SendAsync(request, CancellationToken);
    }

    /// <summary>Has the admin create an invite and returns its display code.</summary>
    public static async Task<string> CreateInviteCodeAsync(this HttpClient client, string adminAccessToken)
    {
        using var response = await client.CreateInviteAsync(adminAccessToken);
        response.EnsureSuccessStatusCode();
        var invite = await response.Content.ReadFromJsonAsync<InviteCodeDto>(CancellationToken);
        return invite!.Code;
    }

    /// <summary>Registers a regular user through an admin-created invite and returns their session.</summary>
    public static async Task<AuthResponseDto> RegisterUserAsync(
        this HttpClient client,
        string adminAccessToken,
        string email)
    {
        var code = await client.CreateInviteCodeAsync(adminAccessToken);
        using var response = await client.RegisterAsync(email, code);
        response.EnsureSuccessStatusCode();
        return await response.ReadAuthAsync();
    }

    public static async Task<AuthResponseDto> SignInAsync(this HttpClient client, string email)
    {
        using var response = await client.LoginAsync(email);
        response.EnsureSuccessStatusCode();
        return await response.ReadAuthAsync();
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

    /// <summary>Sends a refresh with an explicit cookie value, e.g. to replay an old token from a cookieless client.</summary>
    public static async Task<HttpResponseMessage> RefreshWithTokenAsync(this HttpClient client, string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RefreshUri);
        request.Headers.Add("Cookie", $"{RefreshCookieName}={refreshToken}");
        return await client.SendAsync(request, CancellationToken);
    }

    /// <summary>The refresh token value from a Set-Cookie header, or null when the cookie was not set.</summary>
    public static string? ReadRefreshToken(this HttpResponseMessage response)
    {
        var value = response.FindRefreshCookieHeader()?.Split(';')[0].Split('=', 2)[1];
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static string? FindRefreshCookieHeader(this HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.FirstOrDefault(cookie => cookie.StartsWith($"{RefreshCookieName}=", StringComparison.Ordinal))
            : null;

    private static void Authorize(HttpRequestMessage request, string? accessToken)
    {
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}

internal sealed record AuthResponseDto(string AccessToken, DateTimeOffset AccessTokenExpiresAt, CurrentUserDto User);

internal sealed record CurrentUserDto(Guid Id, string Email, string DisplayName, bool IsAdmin);

internal sealed record InviteCodeDto(
    Guid Id,
    string Code,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string Status,
    DateTimeOffset? UsedAt);

internal sealed record InviteCodePageDto(
    List<InviteCodeDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    int ActiveCount);
