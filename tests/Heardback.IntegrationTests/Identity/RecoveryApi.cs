using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Heardback.IntegrationTests.Identity;

/// <summary>Test-side view of the recovery and member endpoints (ADR-0015).</summary>
internal static class RecoveryApi
{
    private const string MembersPath = "/api/admin/members";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task<HttpResponseMessage> ListMembersAsync(this HttpClient client, string accessToken) =>
        client.SendAsync(HttpMethod.Get, MembersPath, accessToken);

    public static Task<HttpResponseMessage> IssueResetCodeAsync(this HttpClient client, string accessToken, Guid memberId) =>
        client.SendAsync(HttpMethod.Post, $"{MembersPath}/{memberId}/reset-code", accessToken);

    public static async Task<ResetCodeDto> IssueResetCodeOkAsync(this HttpClient client, string accessToken, Guid memberId)
    {
        using var response = await client.IssueResetCodeAsync(accessToken, memberId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ResetCodeDto>(CancellationToken))!;
    }

    public static async Task<MemberPageDto> ListMembersOkAsync(this HttpClient client, string accessToken)
    {
        using var response = await client.ListMembersAsync(accessToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MemberPageDto>(CancellationToken))!;
    }

    public static Task<HttpResponseMessage> ResetPasswordAsync(this HttpClient client, string code, string newPassword) =>
        client.PostAsJsonAsync(new Uri("/api/auth/reset-password", UriKind.Relative), new { code, newPassword }, CancellationToken);

    public static Task<HttpResponseMessage> ChangePasswordAsync(
        this HttpClient client,
        string accessToken,
        string currentPassword,
        string newPassword) =>
        client.SendAsync(HttpMethod.Post, "/api/account/password", accessToken, new { currentPassword, newPassword });

    private static async Task<HttpResponseMessage> SendAsync(
        this HttpClient client,
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative))
        {
            Content = body is null ? null : JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await client.SendAsync(request, CancellationToken);
    }
}

internal sealed record ResetCodeDto(string Code, DateTimeOffset ExpiresAt);

internal sealed record MemberDto(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsAdmin,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ResetCodeExpiresAt);

internal sealed record MemberPageDto(List<MemberDto> Items, int Page, int PageSize, int TotalCount, int TotalPages);
