using System.Globalization;
using System.Net;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

public sealed class ApplicationRateLimitTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const int WritesPerMinute = 3;

    protected override void ConfigureSettings(IDictionary<string, string?> settings) =>
        settings["Applications:WritesPerMinute"] = WritesPerMinute.ToString(CultureInfo.InvariantCulture);

    [Fact]
    public async Task Writes_OverTheLimit_Return429WithRetryAfter()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication(url: null));
        using var second = await Client.AddEventAsync(Alice, created.Id, new { occurredOn = Today, note = "one" });
        using var third = await Client.DeleteApplicationAsync(Alice, created.Id);

        using var rejected = await Client.CreateApplicationAsync(Alice, NewApplication(url: null));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
    }

    [Fact]
    public async Task Writes_OneUserOverTheLimit_DoesNotLimitAnotherUser()
    {
        for (var write = 0; write < WritesPerMinute; write++)
        {
            await Client.CreateApplicationOkAsync(Alice, NewApplication(company: $"Company {write}", url: null));
        }

        using var bobs = await Client.CreateApplicationAsync(Bob, NewApplication(url: null));

        Assert.Equal(HttpStatusCode.Created, bobs.StatusCode);
    }

    [Fact]
    public async Task Reads_AfterTheWriteLimit_StillWork()
    {
        for (var write = 0; write < WritesPerMinute; write++)
        {
            await Client.CreateApplicationOkAsync(Alice, NewApplication(company: $"Company {write}", url: null));
        }

        using var list = await Client.ListApplicationsAsync(Alice);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }
}
