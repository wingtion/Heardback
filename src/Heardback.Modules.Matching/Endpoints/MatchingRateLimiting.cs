using System.Threading.RateLimiting;
using Heardback.Shared.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Matching.Endpoints;

/// <summary>
/// Per signed-in user: CV uploads per hour and other CV changes per minute. Reading a PDF is the heaviest work the
/// API does, so the upload limit is low: enough to fix a wrong file a few times, not enough to keep the server
/// busy (ADR-0006). Every upload attempt counts, including rejected files. The host decides how a rejection looks.
/// </summary>
internal static class MatchingRateLimiting
{
    public const string CvUploadPolicy = "matching-cv-uploads";

    /// <summary>Other CV changes: skills marked as not the user's, deletes.</summary>
    public const string CvWritePolicy = "matching-cv-writes";

    private static readonly TimeSpan UploadWindow = TimeSpan.FromHours(1);
    private static readonly TimeSpan WriteWindow = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddMatchingRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<MatchingSettings>>((options, settings) =>
            {
                options.AddPolicy(
                    CvUploadPolicy, context => PerUser(context, settings.Value.CvUploadsPerHour, UploadWindow));
                options.AddPolicy(
                    CvWritePolicy, context => PerUser(context, settings.Value.CvWritesPerMinute, WriteWindow));
            });

        return services;
    }

    // The limiter runs after authorization and uploads require sign-in, so the user id is always known here.
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
