using Heardback.Shared.Demo;

namespace Heardback.Modules.Applications.Samples;

/// <summary>Gives demo accounts their applications (ADR-0018).</summary>
internal sealed class ApplicationsDemoSeeder(SampleApplicationWriter writer, TimeProvider timeProvider) : IDemoDataSeeder
{
    public async Task SeedVisitorAsync(CancellationToken cancellationToken)
    {
        var today = Today();
        foreach (var sample in SampleApplications.All.Concat(DemoApplications.VisitorExtras))
        {
            await writer.AddAsync(sample, today, cancellationToken);
        }
    }

    public async Task SeedNeighborAsync(int number, CancellationToken cancellationToken)
    {
        var today = Today();
        foreach (var application in DemoApplications.Neighbors.Where(application => application.Neighbor == number))
        {
            await writer.AddAsync(DemoApplications.For(application), today, cancellationToken);
        }
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
