using Microsoft.Extensions.DependencyInjection;

namespace Heardback.Modules.Insights;

/// <summary>
/// Registration entry point for the Insights module. Together with the types in
/// <c>Heardback.Modules.Insights.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class InsightsModule
{
    /// <summary>Registers the Insights module's services.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddInsightsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
