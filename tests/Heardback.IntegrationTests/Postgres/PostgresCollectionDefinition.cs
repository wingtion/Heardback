namespace Heardback.IntegrationTests.Postgres;

[CollectionDefinition(Name)]
public sealed class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres";
}
