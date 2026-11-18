using Heardback.IntegrationTests.Postgres;

namespace Heardback.IntegrationTests.Applications;

public sealed class ApplicationBoardTests(PostgresFixture postgres) : ApplicationsTestBase(postgres)
{
    private const int ColumnLimit = 2;

    protected override void ConfigureSettings(IDictionary<string, string?> settings) =>
        settings["Applications:BoardColumnLimit"] = ColumnLimit.ToString(System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public async Task GetBoard_NoApplications_ReturnsFourEmptyColumnsInBoardOrder()
    {
        var board = await Client.GetBoardOkAsync(Alice);

        Assert.Equal(["Applied", "Interview", "Offer", "Rejected"], board.Columns.Select(column => column.Status));
        Assert.All(board.Columns, column => Assert.Equal(0, column.TotalCount));
    }

    [Fact]
    public async Task GetBoard_ApplicationsInSeveralStatuses_GroupsThemWithNoResponseOnAppliedCards()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Old", url: null, appliedOn: Today.AddDays(-15)));
        var interviewing = await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Halden", url: null));
        using var moved = await Client.AddEventAsync(
            Alice, interviewing.Id, new { status = "Interview", occurredOn = Today });

        var board = await Client.GetBoardOkAsync(Alice);

        var applied = Assert.Single(board.Columns[0].Items);
        Assert.Equal(15, applied.NoResponseDays);
        Assert.Equal(["Halden"], board.Columns[1].Items.Select(item => item.Company));
    }

    [Fact]
    public async Task GetBoard_MoreCardsThanTheLimit_ReturnsTheNewestAndTheRealTotal()
    {
        foreach (var company in new[] { "First", "Second", "Third" })
        {
            await Client.CreateApplicationOkAsync(Alice, NewApplication(company: company, url: null));
            Clock.Advance(TimeSpan.FromMinutes(1));
        }

        var applied = (await Client.GetBoardOkAsync(Alice)).Columns[0];

        Assert.Equal(3, applied.TotalCount);
        Assert.Equal(["Third", "Second"], applied.Items.Select(item => item.Company));
    }

    [Fact]
    public async Task GetBoard_SearchAndChannel_FilterTheCardsAndTheTotals()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Northwind", url: null, channel: "Referral"));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Kestrel", url: null, channel: "Referral"));
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Northgate", url: null, channel: "Other"));

        var board = await Client.GetBoardOkAsync(Alice, "?search=north&channel=Referral");

        Assert.Equal(1, board.Columns[0].TotalCount);
        Assert.Equal(["Northwind"], board.Columns[0].Items.Select(item => item.Company));
    }

    [Fact]
    public async Task GetBoard_TwoUsers_EachSeesOnlyTheirOwn()
    {
        await Client.CreateApplicationOkAsync(Alice, NewApplication(company: "Northwind", url: null));
        await Client.CreateApplicationOkAsync(Bob, NewApplication(company: "Kestrel", url: null));

        var board = await Client.GetBoardOkAsync(Bob);

        Assert.Equal(["Kestrel"], board.Columns[0].Items.Select(item => item.Company));
        Assert.Equal(1, board.Columns[0].TotalCount);
    }
}
