using System.ComponentModel.DataAnnotations;

namespace JobTrail.Modules.Identity.Authentication;

/// <summary>Per-client-IP limits for the anonymous auth endpoints, which are the brute-force targets.</summary>
internal sealed class AuthRateLimitSettings
{
    public const string SectionName = "Identity:RateLimits";

    /// <summary>Login and register attempts per window, shared, so switching endpoints does not reset the budget.</summary>
    [Range(1, 1000)]
    public int CredentialAttemptsPerWindow { get; set; } = 10;

    /// <summary>Refreshes per window. Higher, because the app refreshes on its own (startup, expired tokens).</summary>
    [Range(1, 1000)]
    public int RefreshesPerWindow { get; set; } = 30;

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
