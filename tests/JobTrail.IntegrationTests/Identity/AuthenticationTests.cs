using System.Net;
using System.Net.Http.Json;
using JobTrail.IntegrationTests.Api;
using JobTrail.IntegrationTests.Postgres;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace JobTrail.IntegrationTests.Identity;

public sealed class AuthenticationTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    private const string ProblemJson = "application/problem+json";

    [Fact]
    public async Task Register_BootstrapCode_CreatesAdminAndSignsIn()
    {
        using var response = await Client.RegisterAsync("Admin@Example.com", ApiFactory.BootstrapAdminCode);
        var auth = await response.ReadAuthAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(auth.User.IsAdmin);
        Assert.Equal("Admin@Example.com", auth.User.Email);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
    }

    [Fact]
    public async Task Register_Success_SetsHardenedRefreshCookie()
    {
        using var response = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);

        var cookie = response.FindRefreshCookieHeader()?.ToLowerInvariant();

        Assert.NotNull(cookie);
        Assert.Contains("httponly", cookie, StringComparison.Ordinal);
        Assert.Contains("secure", cookie, StringComparison.Ordinal);
        Assert.Contains("samesite=strict", cookie, StringComparison.Ordinal);
        Assert.Contains("path=/api/auth", cookie, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_BootstrapCodeAfterAdminExists_ReturnsInvalidInviteCode()
    {
        await Client.RegisterAdminAsync();

        using var response = await Client.RegisterAsync("second@example.com", ApiFactory.BootstrapAdminCode);

        await AssertValidationProblemAsync(response, "inviteCode");
    }

    [Fact]
    public async Task Register_BootstrapCodeConcurrently_CreatesExactlyOneAdmin()
    {
        var statuses = await Task.WhenAll(Enumerable.Range(0, 5).Select(async i =>
        {
            using var response = await CreateClient().RegisterAsync($"racer{i}@example.com", ApiFactory.BootstrapAdminCode);
            return response.StatusCode;
        }));

        Assert.Single(statuses, status => status == HttpStatusCode.Created);
        Assert.Equal("1", await QuerySingleAsync("SELECT count(*)::text FROM identity.users WHERE is_admin"));
    }

    [Fact]
    public async Task Register_UnknownInviteCode_ReturnsInvalidInviteCode()
    {
        using var response = await Client.RegisterAsync("someone@example.com", "ZZZZ-ZZZZ-ZZZZ");

        await AssertValidationProblemAsync(response, "inviteCode");
    }

    [Fact]
    public async Task Register_PasswordTooShort_ReturnsFieldError()
    {
        using var response = await Client.RegisterAsync(
            "admin@example.com",
            ApiFactory.BootstrapAdminCode,
            password: "short");

        await AssertValidationProblemAsync(response, "password");
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsFieldError()
    {
        using var response = await Client.RegisterAsync("not-an-email", ApiFactory.BootstrapAdminCode);

        await AssertValidationProblemAsync(response, "email");
    }

    [Fact]
    public async Task Login_CorrectCredentialsAnyEmailCasing_ReturnsTokenAndRefreshCookie()
    {
        await Client.RegisterAdminAsync("admin@example.com");

        using var response = await CreateClient().LoginAsync("ADMIN@example.COM");
        var auth = await response.ReadAuthAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("admin@example.com", auth.User.Email);
        Assert.NotNull(response.FindRefreshCookieHeader());
    }

    [Fact]
    public async Task Login_WrongPasswordOrUnknownEmail_ReturnsSameGenericUnauthorized()
    {
        await Client.RegisterAdminAsync("admin@example.com");

        using var wrongPassword = await Client.LoginAsync("admin@example.com", "not the right password");
        using var unknownEmail = await Client.LoginAsync("nobody@example.com");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var wrongPasswordProblem = await wrongPassword.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        var unknownEmailProblem = await unknownEmail.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        Assert.Equal(wrongPasswordProblem?.Detail, unknownEmailProblem?.Detail);
        Assert.Null(wrongPassword.FindRefreshCookieHeader());
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorizedProblem()
    {
        using var response = await Client.GetMeAsync(accessToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(ProblemJson, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetMe_ValidToken_ReturnsOwnProfile()
    {
        var admin = await Client.RegisterAdminAsync("admin@example.com");

        using var response = await Client.GetMeAsync(admin.AccessToken);
        var me = await response.Content.ReadFromJsonAsync<CurrentUserDto>(CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(admin.User, me);
    }

    [Fact]
    public async Task GetMe_AccessTokenPastLifetime_ReturnsUnauthorized()
    {
        var admin = await Client.RegisterAdminAsync();

        Clock.Advance(TimeSpan.FromMinutes(15));
        using var response = await Client.GetMeAsync(admin.AccessToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_TokenSignedWithAnotherKey_ReturnsUnauthorized()
    {
        var admin = await Client.RegisterAdminAsync();
        var forged = ForgeToken(admin.User.Id, key: new byte[32]);

        using var response = await Client.GetMeAsync(forged);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_Success_StoresOnlyHashesOfPasswordAndRefreshToken()
    {
        using var response = await Client.RegisterAsync("admin@example.com", ApiFactory.BootstrapAdminCode);
        var refreshToken = response.FindRefreshCookieHeader()!.Split(';')[0].Split('=', 2)[1];

        var passwordHash = await QuerySingleAsync("SELECT password_hash FROM identity.users");
        var tokenHash = await QuerySingleAsync("SELECT token_hash FROM identity.refresh_tokens");

        Assert.DoesNotContain(IdentityApi.DefaultPassword, passwordHash, StringComparison.Ordinal);
        Assert.NotEqual(refreshToken, tokenHash);
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(field, problem.Errors.Keys);
    }

    private static string ForgeToken(Guid userId, byte[] key) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "jobtrail-api",
            Audience = "jobtrail-web",
            Claims = new Dictionary<string, object> { ["sub"] = userId.ToString(), ["role"] = "admin" },
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
        });

    private async Task<string> QuerySingleAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync(CancellationToken))!;
    }
}
