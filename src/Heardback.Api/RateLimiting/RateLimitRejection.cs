using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Heardback.Api.RateLimiting;

/// <summary>How every rate-limit rejection looks, whichever module owns the policy.</summary>
internal static partial class RateLimitRejection
{
    public static IServiceCollection AddRateLimitRejection(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            // The body is left empty so UseStatusCodePages turns it into a 429 ProblemDetails.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;
        });

    private static ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        // The client IP is deliberately not logged (personal data).
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimitRejection));
        LogRejected(logger, context.HttpContext.Request.Path);

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rate limit exceeded for {Path}")]
    private static partial void LogRejected(ILogger logger, string path);
}
