using System.Threading.RateLimiting;
using Heardback.Shared.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Identity.Authentication;

/// <summary>
/// Per-user limits for account endpoints that are heavy or sensitive: the data export reads everything at once, and
/// reset codes hand over an account. The host decides how a rejection looks.
/// </summary>
internal static class AccountRateLimiting
{
    public const string ExportPolicy = "account-export";
    public const string ResetCodePolicy = "admin-reset-codes";

    public static IServiceCollection AddAccountRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<AuthRateLimitSettings>>((options, settings) =>
            {
                options.AddPolicy(
                    ExportPolicy,
                    context => PerUser(context, settings.Value.DataExportsPerWindow, settings.Value.AccountWindow));
                options.AddPolicy(
                    ResetCodePolicy,
                    context => PerUser(context, settings.Value.ResetCodesPerWindow, settings.Value.AccountWindow));
            });

        return services;
    }

    // The limiter runs after authorization and these endpoints require sign-in, so the user id is always known here.
    private static RateLimitPartition<Guid> PerUser(HttpContext context, int permitLimit, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.RequestServices.GetRequiredService<ICurrentUser>().UserId ?? Guid.Empty,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
            });
}
