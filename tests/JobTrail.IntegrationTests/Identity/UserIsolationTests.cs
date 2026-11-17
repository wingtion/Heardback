using System.Net;
using System.Net.Http.Json;
using JobTrail.IntegrationTests.Postgres;

namespace JobTrail.IntegrationTests.Identity;

/// <summary>Cross-user checks over HTTP: a user only ever sees their own identity data.</summary>
public sealed class UserIsolationTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    [Fact]
    public async Task GetMe_TwoUsers_EachSeesOnlyTheirOwnProfile()
    {
        var admin = await Client.RegisterAdminAsync("admin@example.com");
        var alice = await CreateClient().RegisterUserAsync(admin.AccessToken, "alice@example.com");
        var bob = await CreateClient().RegisterUserAsync(admin.AccessToken, "bob@example.com");

        var aliceMe = await GetProfileAsync(alice.AccessToken);
        var bobMe = await GetProfileAsync(bob.AccessToken);

        Assert.Equal("alice@example.com", aliceMe.Email);
        Assert.Equal("bob@example.com", bobMe.Email);
        Assert.NotEqual(aliceMe.Id, bobMe.Id);
    }

    [Fact]
    public async Task GetMe_IdInQueryString_IsIgnoredInFavourOfToken()
    {
        var admin = await Client.RegisterAdminAsync("admin@example.com");
        var alice = await CreateClient().RegisterUserAsync(admin.AccessToken, "alice@example.com");
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/me?id={admin.User.Id}", UriKind.Relative));
        request.Headers.Authorization = new("Bearer", alice.AccessToken);

        using var response = await Client.SendAsync(request, CancellationToken);
        var me = await response.Content.ReadFromJsonAsync<CurrentUserDto>(CancellationToken);

        Assert.Equal(alice.User.Id, me?.Id);
    }

    [Fact]
    public async Task ListInvites_AsRegularUser_ReturnsForbidden()
    {
        var admin = await Client.RegisterAdminAsync();
        var alice = await CreateClient().RegisterUserAsync(admin.AccessToken, "alice@example.com");

        using var response = await Client.ListInvitesAsync(alice.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<CurrentUserDto> GetProfileAsync(string accessToken)
    {
        using var response = await Client.GetMeAsync(accessToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CurrentUserDto>(CancellationToken))!;
    }
}
