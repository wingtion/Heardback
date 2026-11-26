using Heardback.Modules.Identity.Account;
using Heardback.Modules.Identity.Authentication;
using Heardback.Modules.Identity.CurrentUser;
using Heardback.Modules.Identity.Demo;
using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Endpoints;
using Heardback.Modules.Identity.Invites;
using Heardback.Modules.Identity.Login;
using Heardback.Modules.Identity.Members;
using Heardback.Modules.Identity.Owner;
using Heardback.Modules.Identity.Persistence;
using Heardback.Modules.Identity.Recovery;
using Heardback.Modules.Identity.Registration;
using Heardback.Modules.Identity.Sessions;
using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heardback.Modules.Identity;

/// <summary>
/// Registration entry point for the Identity module. Together with the types in
/// <c>Heardback.Modules.Identity.Contracts</c>, this is the module's only public surface.
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
                $"{InviteSettings.SectionName}:BootstrapAdminCode must be {InviteCode.CodeLength} characters "
                + "from 0-9 and A-Z without I, L, O or U (dashes allowed), like a generated invite code. "
                + "Generate one with: openssl rand -hex 6")
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.TryAddScoped<BackgroundUser>();
        services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
        services.AddModuleDbContext<IdentityDbContext>(IdentityDbContext.Schema);
        services.AddJwtAuthentication();
        services.AddAuthRateLimiting();
        services.AddAccountRateLimiting();

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
        services.AddScoped<AccountRemover>();
        services.AddScoped<DeleteAccountHandler>();
        services.AddScoped<ExportAccountHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<PasswordChanger>();
        services.AddScoped<ResetCodeIssuer>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ListMembersHandler>();
        services.AddScoped<IssueResetCodeHandler>();
        services.AddScoped<OwnerCommandRunner>();
        AddDemo(services);

        return services;
    }

    private static void AddDemo(IServiceCollection services)
    {
        services.AddOptions<DemoSettings>()
            .BindConfiguration(DemoSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IDemoAccounts, DemoAccountsReader>();
        services.AddScoped<DemoAccountScope>();
        services.AddScoped<StartDemoHandler>();
        services.AddScoped<DemoAccountCleaner>();
        services.AddScoped<DemoNeighborProvisioner>();
        services.AddHostedService<DemoMaintenanceService>();
    }

    /// <summary>
    /// Maps the Identity module's HTTP endpoints (<c>/api/auth/*</c>, <c>/api/me</c>, <c>/api/account</c>, <c>/api/admin/invite-codes</c>, <c>/api/admin/members</c>).
    /// </summary>
    /// <param name="routes">The application's endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapAuthEndpoints();
        routes.MapCurrentUserEndpoints();
        routes.MapAccountEndpoints();
        routes.MapInviteCodeEndpoints();
        routes.MapMemberEndpoints();
        routes.MapDemoEndpoints();

        return routes;
    }

    /// <summary>
    /// Runs an owner command (<c>reset-code &lt;email&gt;</c> or <c>list-admins</c>) when the process was started with
    /// one, instead of the web server (ADR-0015).
    /// </summary>
    /// <param name="services">The built application's services.</param>
    /// <param name="args">The process arguments.</param>
    /// <param name="output">Where the command writes its result, normally the console.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The process exit code, or <see langword="null"/> when the arguments are not an owner command.</returns>
    public static async Task<int?> TryRunOwnerCommandAsync(
        this IServiceProvider services,
        IReadOnlyList<string> args,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);

        if (!OwnerCommandRunner.IsCommand(args))
        {
            return null;
        }

        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OwnerCommandRunner>()
            .RunAsync(args, output, cancellationToken);
    }
}
