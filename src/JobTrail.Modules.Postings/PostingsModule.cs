using Microsoft.Extensions.DependencyInjection;

namespace JobTrail.Modules.Postings;

/// <summary>
/// Registration entry point for the Postings module. Together with the types in
/// <c>JobTrail.Modules.Postings.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class PostingsModule
{
    /// <summary>Registers the Postings module's services.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddPostingsModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
