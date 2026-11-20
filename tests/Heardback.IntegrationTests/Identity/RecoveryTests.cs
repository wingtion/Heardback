using System.Globalization;
using System.Net;
using Heardback.IntegrationTests.Applications;
using Heardback.IntegrationTests.Postgres;
using Heardback.Modules.Identity;

namespace Heardback.IntegrationTests.Identity;

/// <summary>Account recovery without email: admin-issued and owner-issued reset codes, password change (ADR-0015).</summary>
public sealed class RecoveryTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const string NewPassword = "a brand new passphrase";
    private const string BobEmail = "bob@example.com";
    private const int ResetCodesPerWindow = 3;

    protected override void ConfigureSettings(IDictionary<string, string?> settings)
    {
        settings["Identity:RateLimits:ResetCodesPerWindow"] = ResetCodesPerWindow.ToString(CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task ListMembers_Admin_SeesNamesEmailsAndAdmins()
    {
        var members = await Client.ListMembersOkAsync(Alice);

        Assert.Equal(["alice@example.com", BobEmail], members.Items.Select(member => member.Email));
        Assert.Equal([true, false], members.Items.Select(member => member.IsAdmin));
        Assert.All(members.Items, member => Assert.Null(member.ResetCodeExpiresAt));
    }

    [Fact]
    public async Task MemberEndpoints_NotAnAdmin_Return403()
    {
        using var list = await Client.ListMembersAsync(Bob);
        using var issue = await Client.IssueResetCodeAsync(Bob, await BobIdAsync());

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, issue.StatusCode);
    }

    [Fact]
    public async Task IssueResetCode_ForAMember_IsValidFor24HoursAndShownInTheList()
    {
        var code = await Client.IssueResetCodeOkAsync(Alice, await BobIdAsync());

        Assert.Matches("^[0-9A-Z]{4}-[0-9A-Z]{4}-[0-9A-Z]{4}$", code.Code);
        Assert.Equal(Clock.GetUtcNow().AddHours(24), code.ExpiresAt, TimeSpan.FromMilliseconds(1));
        var bob = (await Client.ListMembersOkAsync(Alice)).Items.Single(member => member.Email == BobEmail);
        Assert.Equal(code.ExpiresAt, bob.ResetCodeExpiresAt!.Value, TimeSpan.FromMilliseconds(1)); // µs in Postgres
    }

    [Fact]
    public async Task IssueResetCode_StoresOnlyAHash()
    {
        var code = await Client.IssueResetCodeOkAsync(Alice, await BobIdAsync());
        var plain = code.Code.Replace("-", string.Empty, StringComparison.Ordinal);

        Assert.Equal(1, await CountRowsAsync("identity.password_reset_codes"));
        Assert.Equal(0, await CountRowsAsync(
            $"identity.password_reset_codes AS c WHERE c::text ILIKE '%{plain}%'"));
    }

    [Fact]
    public async Task IssueResetCode_ForAnAdmin_Returns403()
    {
        var aliceId = (await Client.ListMembersOkAsync(Alice)).Items.Single(member => member.IsAdmin).Id;

        using var response = await Client.IssueResetCodeAsync(Alice, aliceId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, await CountRowsAsync("identity.password_reset_codes"));
    }

    [Fact]
    public async Task IssueResetCode_UnknownMember_Returns404()
    {
        using var response = await Client.IssueResetCodeAsync(Alice, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task IssueResetCode_Again_ReplacesTheOldCode()
    {
        var bobId = await BobIdAsync();
        var first = await Client.IssueResetCodeOkAsync(Alice, bobId);
        var second = await Client.IssueResetCodeOkAsync(Alice, bobId);

        using var withFirst = await Client.ResetPasswordAsync(first.Code, NewPassword);
        using var withSecond = await Client.ResetPasswordAsync(second.Code, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, withFirst.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, withSecond.StatusCode);
    }

    [Fact]
    public async Task IssueResetCode_OverThePerAdminLimit_Returns429()
    {
        var bobId = await BobIdAsync();
        for (var attempt = 0; attempt < ResetCodesPerWindow; attempt++)
        {
            using var allowed = await Client.IssueResetCodeAsync(Alice, bobId);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using var refused = await Client.IssueResetCodeAsync(Alice, bobId);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidCode_NewPasswordWorksAndTheOldOneDoesNot()
    {
        var code = await Client.IssueResetCodeOkAsync(Alice, await BobIdAsync());

        using var reset = await CreateClient().ResetPasswordAsync(code.Code.ToLowerInvariant(), NewPassword);
        using var withOld = await CreateClient().LoginAsync(BobEmail);
        using var withNew = await CreateClient().LoginAsync(BobEmail, NewPassword);

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_EndsEverySessionOfTheUser()
    {
        var bobBrowser = CreateClient();
        var bob = await bobBrowser.SignInAsync(BobEmail);
        var code = await Client.IssueResetCodeOkAsync(Alice, await BobIdAsync());
        Clock.Advance(TimeSpan.FromSeconds(1));

        using var reset = await CreateClient().ResetPasswordAsync(code.Code, NewPassword);
        using var oldAccessToken = await Client.ListApplicationsAsync(bob.AccessToken);
        using var oldRefresh = await bobBrowser.RefreshAsync();
        using var aliceStillSignedIn = await Client.ListApplicationsAsync(Alice);

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldAccessToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefresh.StatusCode);
        Assert.Equal(HttpStatusCode.OK, aliceStillSignedIn.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_SameCodeTwice_SecondTimeFails()
    {
        var code = await Client.IssueResetCodeOkAsync(Alice, await BobIdAsync());

        using var first = await CreateClient().ResetPasswordAsync(code.Code, NewPassword);
        using var second = await CreateClient().ResetPasswordAsync(code.Code, "yet another passphrase");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        using var login = await CreateClient().LoginAsync(BobEmail, NewPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_SameCodeAtOnce_OnlyOneSucceeds()
    {
        var code = await Client.IssueResetCodeOkAsync(Alice, await BobIdAsync());

        var attempts = Enumerable.Range(0, 4)
            .Select(attempt => CreateClient().ResetPasswordAsync(code.Code, $"{NewPassword} {attempt}"))
            .ToList();
        var responses = await Task.WhenAll(attempts);
        var statuses = responses.Select(response => response.StatusCode).ToList();
        foreach (var response in responses)
        {
            response.Dispose();
        }

        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.NoContent));
        Assert.All(statuses, status => Assert.Contains(status, new[] { HttpStatusCode.NoContent, HttpStatusCode.BadRequest }));
    }

    [Fact]
    public async Task ResetPassword_After24Hours_Fails()
    {
        var code = await Client.IssueResetCodeOkAsync(Alice, await BobIdAsync());
        Clock.Advance(TimeSpan.FromHours(24));

        using var response = await CreateClient().ResetPasswordAsync(code.Code, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("ABCD-EFGH-JKMN")]
    [InlineData("not a code")]
    public async Task ResetPassword_UnknownOrMalformedCode_GivesTheSameFieldError(string code)
    {
        using var response = await CreateClient().ResetPasswordAsync(code, NewPassword);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("This reset code is invalid, expired or already used.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResetPassword_TooShortNewPassword_Returns400AndKeepsTheCode()
    {
        var code = await Client.IssueResetCodeOkAsync(Alice, await BobIdAsync());

        using var tooShort = await CreateClient().ResetPasswordAsync(code.Code, "short");
        using var valid = await CreateClient().ResetPasswordAsync(code.Code, NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, valid.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrentPassword_KeepsThisBrowserSignedInAndEndsTheRest()
    {
        var otherDevice = CreateClient();
        var old = await otherDevice.SignInAsync(BobEmail);
        Clock.Advance(TimeSpan.FromSeconds(1));
        var thisBrowser = CreateClient();
        var current = await thisBrowser.SignInAsync(BobEmail);

        using var change = await thisBrowser.ChangePasswordAsync(current.AccessToken, IdentityApi.DefaultPassword, NewPassword);
        var session = await change.ReadAuthAsync();
        using var withNewToken = await Client.ListApplicationsAsync(session.AccessToken);
        using var withOldToken = await Client.ListApplicationsAsync(old.AccessToken);
        using var refreshHere = await thisBrowser.RefreshAsync();
        using var refreshElsewhere = await otherDevice.RefreshAsync();

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        Assert.Equal(HttpStatusCode.OK, withNewToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, withOldToken.StatusCode);
        Assert.Equal(HttpStatusCode.OK, refreshHere.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshElsewhere.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400OnCurrentPassword()
    {
        using var response = await Client.ChangePasswordAsync(Bob, "not the current password", NewPassword);
        var body = await response.Content.ReadAsStringAsync(CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("currentPassword", body, StringComparison.Ordinal);
        using var login = await CreateClient().LoginAsync(BobEmail);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task OwnerResetCode_ForAnAdmin_PrintsACodeThatWorks()
    {
        using var output = new StringWriter();

        var exitCode = await Factory.Services.TryRunOwnerCommandAsync(
            ["reset-code", "ALICE@example.com"], output, CancellationToken);
        var code = output.ToString().Split(["Reset code: "], StringSplitOptions.None)[1][..14];
        using var reset = await CreateClient().ResetPasswordAsync(code, NewPassword);

        Assert.Equal(0, exitCode);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        using var login = await CreateClient().LoginAsync("alice@example.com", NewPassword);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task OwnerResetCode_UnknownEmail_FailsWithoutACode()
    {
        using var output = new StringWriter();

        var exitCode = await Factory.Services.TryRunOwnerCommandAsync(
            ["reset-code", "nobody@example.com"], output, CancellationToken);

        Assert.Equal(1, exitCode);
        Assert.Equal(0, await CountRowsAsync("identity.password_reset_codes"));
    }

    [Fact]
    public async Task OwnerListAdmins_PrintsOnlyAdmins()
    {
        using var output = new StringWriter();

        var exitCode = await Factory.Services.TryRunOwnerCommandAsync(["list-admins"], output, CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Contains("<alice@example.com>", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(BobEmail, output.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(new string[0], null)]
    [InlineData(new[] { "--urls", "http://localhost:5000" }, null)]
    [InlineData(new[] { "reset-code" }, 2)]
    public async Task TryRunOwnerCommand_OtherArguments_StartTheServerOrPrintUsage(string[] args, int? expected)
    {
        using var output = new StringWriter();

        var exitCode = await Factory.Services.TryRunOwnerCommandAsync(args, output, CancellationToken);

        Assert.Equal(expected, exitCode);
    }

    private async Task<Guid> BobIdAsync() =>
        (await Client.ListMembersOkAsync(Alice)).Items.Single(member => member.Email == BobEmail).Id;
}
