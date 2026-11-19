using Heardback.Modules.Insights.Dashboard;
using Heardback.Modules.Insights.Endpoints;
using Heardback.Modules.Insights.FollowUps;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.Modules.Insights;

/// <summary>
/// Registration entry point for the Insights module. Together with the types in
/// <c>Heardback.Modules.Insights.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class InsightsModule
{
    /// <summary>Registers the Insights module's services. Needs the Applications module.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddInsightsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<GetDashboardHandler>();
        services.AddScoped<ListFollowUpsHandler>();

        return services;
    }

    /// <summary>Maps the Insights module's HTTP endpoints (<c>/api/insights/*</c>).</summary>
    /// <param name="routes">The application's endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapInsightRoutes();

        return routes;
    }
}
