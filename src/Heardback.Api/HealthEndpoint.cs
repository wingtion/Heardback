namespace Heardback.Api;

internal static class HealthEndpoint
{
    /// <summary>Called by the host's health check directly, never through the frontend proxy.</summary>
    public const string Path = "/health";
}
