using System.Threading.RateLimiting;
using Heardback.Shared.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Matching.Endpoints;

/// <summary>
/// CV uploads per signed-in user per hour. Reading a PDF is the heaviest work the API does, so the limit is low:
/// enough to fix a wrong file a few times, not enough to keep the server busy (ADR-0006). Every attempt counts,
/// including rejected files. The host decides how a rejection looks.
/// </summary>
internal static class MatchingRateLimiting
{
    public const string CvUploadPolicy = "matching-cv-uploads";

    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    public static IServiceCollection AddMatchingRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<MatchingSettings>>((options, settings) =>
                options.AddPolicy(CvUploadPolicy, context => PerUser(context, settings.Value.CvUploadsPerHour)));

        return services;
    }

    // The limiter runs after authorization and uploads require sign-in, so the user id is always known here.
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
