using System.Net;
using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

/// <summary>Enum fields only accept their names; numbers and unknown values never reach the database.</summary>
public sealed class ApplicationEnumValidationTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    [Fact]
    public async Task Create_NumericChannel_Returns400AndStoresNothing()
    {
        using var response = await Client.CreateApplicationAsync(
            Alice, new { company = "Northwind", role = "Dev", channel = 42, appliedOn = Today });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountRowsAsync("applications.applications"));
    }

    [Fact]
    public async Task Create_NumericValueOfARealChannel_Returns400()
    {
        using var response = await Client.CreateApplicationAsync(
            Alice, new { company = "Northwind", role = "Dev", channel = 1, appliedOn = Today });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddEvent_UnknownStatusNumber_Returns400AndAddsNothing()
    {
        var created = await Client.CreateApplicationOkAsync(Alice, NewApplication());

        using var response = await Client.AddEventAsync(Alice, created.Id, new { status = 99, occurredOn = Today });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, await CountRowsAsync("applications.application_events"));
    }

    [Theory]
    [InlineData("?status=7")]
    [InlineData("?channel=9")]
    [InlineData("?status=Ghosted")]
    public async Task List_UndefinedFilterValue_Returns400(string query)
    {
        using var response = await Client.ListApplicationsAsync(Alice, query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBoard_UndefinedChannel_Returns400()
    {
        using var response = await Client.ListApplicationsAsync(Alice, "/board?channel=9");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
