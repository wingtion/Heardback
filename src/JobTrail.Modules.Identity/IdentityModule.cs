using JobTrail.Modules.Identity.Authentication;
using JobTrail.Modules.Identity.CurrentUser;
using JobTrail.Modules.Identity.Domain;
using JobTrail.Modules.Identity.Endpoints;
using JobTrail.Modules.Identity.Invites;
using JobTrail.Modules.Identity.Login;
using JobTrail.Modules.Identity.Persistence;
using JobTrail.Modules.Identity.Registration;
using JobTrail.Modules.Identity.Sessions;
using JobTrail.Shared.Persistence;
using JobTrail.Shared.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace JobTrail.Modules.Identity;

/// <summary>
/// Registration entry point for the Identity module. Together with the types in
/// <c>JobTrail.Modules.Identity.Contracts</c>, this is the module's only public surface.
/// </summary>
public static class IdentityModule
{
    /// <summary>Registers the Identity module's services, including JWT authentication and the admin policy.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<InviteSettings>()
            .BindConfiguration(InviteSettings.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                settings => settings.HasValidBootstrapCode(),
                $"{InviteSettings.SectionName}:BootstrapAdminCode must have {InviteCode.CodeLength} to "
                + $"{InviteCode.MaxInputLength} characters.")
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
        services.AddModuleDbContext<IdentityDbContext>(IdentityDbContext.Schema);
        services.AddJwtAuthentication();
        services.AddAuthRateLimiting();

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<RefreshTokenRevoker>();
        services.AddScoped<RefreshSessionHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<GetCurrentUserHandler>();
        services.AddScoped<CreateInviteCodeHandler>();
        services.AddScoped<ListInviteCodesHandler>();

        return services;
    }

    /// <summary>
    /// Maps the Identity module's HTTP endpoints (<c>/api/auth/*</c>, <c>/api/me</c>, <c>/api/admin/invite-codes</c>).
    /// </summary>
    /// <param name="routes">The application's endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapAuthEndpoints();
        routes.MapCurrentUserEndpoints();
        routes.MapInviteCodeEndpoints();

        return routes;
    }
}
