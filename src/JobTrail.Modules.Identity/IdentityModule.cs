using JobTrail.Modules.Identity.Authentication;
using JobTrail.Modules.Identity.Persistence;
using JobTrail.Shared.Persistence;
using JobTrail.Shared.Users;
using Microsoft.Extensions.DependencyInjection;

namespace JobTrail.Modules.Identity;

/// <summary>
/// Registration entry point for the Identity module. Together with the types in
/// <c>JobTrail.Modules.Identity.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class IdentityModule
{
    /// <summary>Registers the Identity module's services.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
        services.AddModuleDbContext<IdentityDbContext>(IdentityDbContext.Schema);

        return services;
    }
}
