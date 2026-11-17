using Microsoft.Extensions.DependencyInjection;

namespace JobTrail.Modules.Applications;

/// <summary>
/// Registration entry point for the Applications module. Together with the types in
/// <c>JobTrail.Modules.Applications.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class ApplicationsModule
{
    /// <summary>Registers the Applications module's services.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplicationsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
