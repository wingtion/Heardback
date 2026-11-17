using Microsoft.Extensions.DependencyInjection;

namespace JobTrail.Modules.Matching;

/// <summary>
/// Registration entry point for the Matching module. Together with the types in
/// <c>JobTrail.Modules.Matching.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class MatchingModule
{
    /// <summary>Registers the Matching module's services.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddMatchingModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
