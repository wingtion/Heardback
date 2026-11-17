using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heardback.IntegrationTests.Persistence;
using Heardback.IntegrationTests.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.IntegrationTests.Identity;

/// <summary>
/// Phase 1 "done when": a user cannot read another user's data, proven through the whole stack
/// (access token, ClaimsCurrentUser, ModuleDbContext filter) rather than against the DbContext alone.
/// </summary>
public sealed class EndToEndIsolationTests(PostgresFixture postgres) : IdentityTestBase(postgres)
{
    protected override void ConfigureServices(IServiceCollection services) =>
        IsolationTestEndpoints.Register(services, ConnectionString);

    [Fact]
    public async Task Notes_TwoUsers_EachReadsOnlyTheirOwnThroughTheRealPipeline()
    {
        await CreateNotesTableAsync();
        var admin = await Client.RegisterAdminAsync("admin@example.com");
        var alice = await CreateClient().RegisterUserAsync(admin.AccessToken, "alice@example.com");
        var bob = await CreateClient().RegisterUserAsync(admin.AccessToken, "bob@example.com");

        await PostNoteAsync(alice.AccessToken, "alice's secret");
        await PostNoteAsync(bob.AccessToken, "bob's secret");

        Assert.Equal(["alice's secret"], await GetNotesAsync(alice.AccessToken));
        Assert.Equal(["bob's secret"], await GetNotesAsync(bob.AccessToken));
        Assert.Empty(await GetNotesAsync(admin.AccessToken));
    }

    [Fact]
    public async Task Notes_Anonymous_IsRejectedByTheFallbackPolicy()
    {
        using var response = await Client.GetAsync(new Uri(IsolationTestEndpoints.Path, UriKind.Relative), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task CreateNotesTableAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IsolationTestDbContext>();
        await dbContext.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(CancellationToken);
    }

    private async Task PostNoteAsync(string accessToken, string text)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"{IsolationTestEndpoints.Path}?text={Uri.EscapeDataString(text)}", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await Client.SendAsync(request, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<List<string>> GetNotesAsync(string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(IsolationTestEndpoints.Path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await Client.SendAsync(request, CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<string>>(CancellationToken))!;
    }
}
