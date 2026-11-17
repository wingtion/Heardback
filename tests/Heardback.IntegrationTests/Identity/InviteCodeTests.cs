using System.Net;
using System.Net.Http.Json;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Identity;

public sealed class InviteCodeTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    [Fact]
    public async Task CreateInvite_AsAdmin_ReturnsActiveFormattedCodeWithDefaultExpiry()
    {
        var admin = await Client.RegisterAdminAsync();

        using var response = await Client.CreateInviteAsync(admin.AccessToken);
        var invite = await response.Content.ReadFromJsonAsync<InviteCodeDto>(CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(invite);
        Assert.Matches("^[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}$", invite.Code);
        Assert.Equal("Active", invite.Status);
        Assert.Equal(TimeSpan.FromDays(7), invite.ExpiresAt - invite.CreatedAt);
    }

    [Fact]
    public async Task CreateInvite_AsRegularUser_ReturnsForbidden()
    {
        var admin = await Client.RegisterAdminAsync();
        var user = await Client.RegisterUserAsync(admin.AccessToken, "user@example.com");

        using var response = await Client.CreateInviteAsync(user.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvite_Anonymous_ReturnsUnauthorized()
    {
        using var response = await Client.CreateInviteAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvite_ExpiryOutOfRange_ReturnsFieldError()
    {
        var admin = await Client.RegisterAdminAsync();

        using var response = await Client.CreateInviteAsync(admin.AccessToken, expiresInDays: 365);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        Assert.Contains("expiresInDays", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListInvites_MixedStates_ReturnsNewestFirstWithStatus()
    {
        var admin = await Client.RegisterAdminAsync("admin@example.com");
        using var expiring = await Client.CreateInviteAsync(admin.AccessToken, expiresInDays: 1);
        Clock.Advance(TimeSpan.FromMinutes(1));
        var usedCode = await Client.CreateInviteCodeAsync(admin.AccessToken);
        using var registered = await CreateClient().RegisterAsync("user@example.com", usedCode);

        // A day later the first code has expired (and so has the admin's access token).
        Clock.Advance(TimeSpan.FromDays(1));
        var adminToken = (await CreateClient().SignInAsync("admin@example.com")).AccessToken;
        var activeCode = await Client.CreateInviteCodeAsync(adminToken);

        using var response = await Client.ListInvitesAsync(adminToken);
        var page = await response.Content.ReadFromJsonAsync<PagedDto<InviteCodeDto>>(CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal([activeCode, usedCode], page.Items.Take(2).Select(invite => invite.Code));
        Assert.Equal(["Active", "Used", "Expired"], page.Items.Select(invite => invite.Status));
    }

    [Fact]
    public async Task ListInvites_PageSizeAboveMaximum_ReturnsFieldError()
    {
        var admin = await Client.RegisterAdminAsync();

        using var response = await Client.ListInvitesAsync(admin.AccessToken, "?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);
        Assert.Contains("pageSize", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListInvites_SecondPage_ReturnsRemainingItems()
    {
        var admin = await Client.RegisterAdminAsync();
        for (var i = 0; i < 3; i++)
        {
            await Client.CreateInviteCodeAsync(admin.AccessToken);
        }

        using var response = await Client.ListInvitesAsync(admin.AccessToken, "?page=2&pageSize=2");
        var page = await response.Content.ReadFromJsonAsync<PagedDto<InviteCodeDto>>(CancellationToken);

        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task Register_AdminCreatedInvite_CreatesRegularUser()
    {
        var admin = await Client.RegisterAdminAsync();

        var user = await CreateClient().RegisterUserAsync(admin.AccessToken, "user@example.com");

        Assert.False(user.User.IsAdmin);
    }

    [Fact]
    public async Task Register_InviteCodeTypedLowercaseWithoutDashes_IsAccepted()
    {
        var admin = await Client.RegisterAdminAsync();
        var code = await Client.CreateInviteCodeAsync(admin.AccessToken);

        using var response = await CreateClient().RegisterAsync(
            "user@example.com",
            code.Replace("-", " ", StringComparison.Ordinal).ToLowerInvariant());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_InviteAlreadyUsed_ReturnsInvalidInviteCode()
    {
        var admin = await Client.RegisterAdminAsync();
        var code = await Client.CreateInviteCodeAsync(admin.AccessToken);
        using var first = await CreateClient().RegisterAsync("first@example.com", code);

        using var second = await CreateClient().RegisterAsync("second@example.com", code);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_InviteWhoseUserWasDeleted_StaysUsed()
    {
        var admin = await Client.RegisterAdminAsync();
        var code = await Client.CreateInviteCodeAsync(admin.AccessToken);
        using var first = await CreateClient().RegisterAsync("leaver@example.com", code);

        // Account deletion arrives in Phase 7; the FK sets used_by_user_id to NULL when it happens.
        await ExecuteSqlAsync("DELETE FROM identity.users WHERE normalized_email = 'LEAVER@EXAMPLE.COM'");
        using var reuse = await CreateClient().RegisterAsync("someone-else@example.com", code);
        using var list = await Client.ListInvitesAsync(admin.AccessToken);
        var page = await list.Content.ReadFromJsonAsync<PagedDto<InviteCodeDto>>(CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, reuse.StatusCode);
        Assert.Equal("Used", Assert.Single(page!.Items).Status);
    }

    [Fact]
    public async Task Register_InviteExpired_ReturnsInvalidInviteCode()
    {
        var admin = await Client.RegisterAdminAsync();
        using var created = await Client.CreateInviteAsync(admin.AccessToken, expiresInDays: 1);
        var invite = await created.Content.ReadFromJsonAsync<InviteCodeDto>(CancellationToken);

        Clock.Advance(TimeSpan.FromDays(1));
        using var response = await CreateClient().RegisterAsync("late@example.com", invite!.Code);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_EmailAlreadyRegistered_ReturnsConflictAndKeepsInviteUnused()
    {
        var admin = await Client.RegisterAdminAsync("admin@example.com");
        var code = await Client.CreateInviteCodeAsync(admin.AccessToken);

        using var duplicate = await CreateClient().RegisterAsync("ADMIN@example.com", code);
        using var retry = await CreateClient().RegisterAsync("new@example.com", code);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
    }

    [Fact]
    public async Task Register_SameInviteConcurrently_OnlyOneSucceeds()
    {
        var admin = await Client.RegisterAdminAsync();
        var code = await Client.CreateInviteCodeAsync(admin.AccessToken);

        var attempts = await Task.WhenAll(Enumerable.Range(0, 5).Select(async i =>
        {
            using var response = await CreateClient().RegisterAsync($"racer{i}@example.com", code);
            return response.StatusCode;
        }));

        Assert.Single(attempts, status => status == HttpStatusCode.Created);
    }
}
