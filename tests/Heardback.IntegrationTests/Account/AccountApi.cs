using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heardback.IntegrationTests.Identity;

namespace Heardback.IntegrationTests.Account;

/// <summary>Test-side view of the account HTTP API (ADR-0015).</summary>
internal static class AccountApi
{
    private static readonly Uri AccountUri = new("/api/account", UriKind.Relative);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static async Task<HttpResponseMessage> DeleteAccountAsync(
        this HttpClient client,
        string? accessToken,
        string password = IdentityApi.DefaultPassword)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, AccountUri)
        {
            Content = JsonContent.Create(new { password }),
        };
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, CancellationToken);
    }
}
