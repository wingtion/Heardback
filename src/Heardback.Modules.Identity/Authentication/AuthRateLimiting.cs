using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Identity.Authentication;

/// <summary>
/// Fixed-window limits per client IP for the auth endpoints. The host decides how a rejection looks
/// (429 ProblemDetails with Retry-After) and runs the middleware.
/// </summary>
internal static class AuthRateLimiting
{
    public const string CredentialsPolicy = "identity-credentials";
    public const string RefreshPolicy = "identity-refresh";

    private const string UnknownClient = "unknown";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<AuthRateLimitSettings>()
            .BindConfiguration(AuthRateLimitSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<AuthRateLimitSettings>>((options, settings) =>
            {
                options.AddPolicy(
                    CredentialsPolicy,
                    context => PerClientIp(context, settings.Value.CredentialAttemptsPerWindow, settings.Value.Window));
                options.AddPolicy(
                    RefreshPolicy,
                    context => PerClientIp(context, settings.Value.RefreshesPerWindow, settings.Value.Window));
            });

        return services;
    }

    // Fixed windows report exactly when the next attempt is allowed, which becomes the Retry-After header.
    private static RateLimitPartition<string> PerClientIp(HttpContext context, int permitLimit, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? UnknownClient,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
            });
}
