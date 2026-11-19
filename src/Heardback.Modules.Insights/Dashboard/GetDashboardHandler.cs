using Heardback.Modules.Applications.Contracts;

namespace Heardback.Modules.Insights.Dashboard;

internal sealed class GetDashboardHandler(IApplicationHistory history, TimeProvider timeProvider)
{
    /// <summary>Worked out on every read from the user's own applications; nothing is stored (ADR-0011).</summary>
    public async Task<DashboardResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var applications = await history.ListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return DashboardCalculator.Calculate(applications, today);
    }
}
