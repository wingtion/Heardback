namespace JobTrail.IntegrationTests.Postgres;

[Collection(PostgresCollectionDefinition.Name)]
public sealed class PostgresFixtureTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Container_StartedFromComposeImage_RunsSameServerVersion()
    {
        // "postgres:18.7-alpine" -> "18.7"
        var expectedVersion = fixture.Image.Split(':')[1].Split('-')[0];

        var result = await fixture.Container.ExecScriptAsync(
            "SHOW server_version;",
            TestContext.Current.CancellationToken);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(expectedVersion, result.Stdout, StringComparison.Ordinal);
    }
}
