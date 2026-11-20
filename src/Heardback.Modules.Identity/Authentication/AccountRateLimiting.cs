using System.Threading.RateLimiting;
using Heardback.Shared.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Identity.Authentication;

/// <summary>
/// Per-user limits for account endpoints that are heavy for the database, such as the data export, which reads
/// everything at once. The host decides how a rejection looks.
/// </summary>
internal static class AccountRateLimiting
{
    public const string ExportPolicy = "account-export";

    public static IServiceCollection AddAccountRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<AuthRateLimitSettings>>((options, settings) =>
                options.AddPolicy(
                    ExportPolicy,
                    context => PerUser(context, settings.Value.DataExportsPerWindow, settings.Value.DataExportWindow)));

        return services;
    }

    // The limiter runs after authorization and the export requires sign-in, so the user id is always known here.
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
