using Heardback.Modules.Matching.Cvs.Domain;
using Heardback.Modules.Matching.Persistence;
using Heardback.Shared.Demo;

namespace Heardback.Modules.Matching.Cvs;

/// <summary>
/// Gives a demo visitor a CV, so the skills match and CV suggestions work straight away (ADR-0018). The person is
/// made up and the text has no contact details. Neighbors need no CV: only their applications are ever read.
/// </summary>
internal sealed class MatchingDemoSeeder(MatchingDbContext dbContext, TimeProvider timeProvider) : IDemoDataSeeder
{
    private const string SampleCvText = """
        Alex Demir
        Backend Developer

        Summary
        Backend developer with three years of experience building web APIs in C# and .NET. Enjoys clean
        architecture, automated tests and making slow queries fast.

        Experience
        Backend Developer, Harbor Commerce (2023 - present)
        - Built ASP.NET Core Web APIs with Entity Framework Core and PostgreSQL for an online marketplace.
        - Moved background jobs to a Postgres-backed queue and cut order processing time by 40%.
        - Wrote unit and integration tests with xUnit and Testcontainers; set up CI with GitHub Actions.
        - Containerised the services with Docker and deployed them to Azure.

        Junior Software Developer, Lumen Labs (2022 - 2023)
        - Maintained an Angular and TypeScript admin panel and its REST API.
        - Added structured logging with Serilog and dashboards in Grafana.

        Education
        BSc Computer Engineering, 2022

        Skills
        C#, .NET, ASP.NET Core, Entity Framework Core, PostgreSQL, SQL Server, Docker, Azure, Git,
        GitHub Actions, xUnit, Angular, TypeScript, REST, Redis

        Languages
        Turkish (native), English (fluent)
        """;

    public async Task SeedVisitorAsync(CancellationToken cancellationToken)
    {
        dbContext.Cvs.Add(Cv.Create(SampleCvText, pageCount: 1, timeProvider.GetUtcNow()));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SeedNeighborAsync(int number, CancellationToken cancellationToken) => Task.CompletedTask;
}
