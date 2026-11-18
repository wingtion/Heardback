using System.Threading.RateLimiting;
using Heardback.Shared.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Applications.Endpoints;

/// <summary>
/// A fixed-window limit on writes per signed-in user, so one account cannot flood the shared posting catalogue
/// or the database. Far above what a person does by hand. The host decides how a rejection looks.
/// </summary>
internal static class ApplicationsRateLimiting
{
    public const string WritePolicy = "applications-writes";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddApplicationsRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<ApplicationsSettings>>((options, settings) =>
                options.AddPolicy(WritePolicy, context => PerUser(context, settings.Value.WritesPerMinute)));

        return services;
    }

    // Writes require sign-in, so a missing id never reaches a handler; it gets its own tiny bucket regardless.
    private static RateLimitPartition<Guid> PerUser(HttpContext context, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.RequestServices.GetRequiredService<ICurrentUser>().UserId ?? Guid.Empty,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = Window,
                QueueLimit = 0,
            });
}
