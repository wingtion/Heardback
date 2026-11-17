using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Heardback.IntegrationTests.Api;

/// <summary>Makes the health endpoint fail unexpectedly, to exercise the global exception handler.</summary>
internal sealed class ThrowingHealthCheckService : HealthCheckService
{
    public const string SensitiveMessage = "Host=db;Password=super-secret";

    public override Task<HealthReport> CheckHealthAsync(
        Func<HealthCheckRegistration, bool>? predicate,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(SensitiveMessage);
}
