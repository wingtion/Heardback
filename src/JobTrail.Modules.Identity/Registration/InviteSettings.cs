using System.ComponentModel.DataAnnotations;
using JobTrail.Modules.Identity.Domain;

namespace JobTrail.Modules.Identity.Registration;

internal sealed class InviteSettings
{
    public const string SectionName = "Identity:Invites";

    /// <summary>
    /// One-time code that registers the first admin. Ignored as soon as an admin exists.
    /// User-secrets locally, environment variable in production; leave unset once the admin is created.
    /// </summary>
    public string? BootstrapAdminCode { get; set; }

    [Range(typeof(TimeSpan), "01:00:00", "90.00:00:00")]
    public TimeSpan DefaultLifetime { get; set; } = TimeSpan.FromDays(7);

    public string? NormalizedBootstrapAdminCode =>
        string.IsNullOrWhiteSpace(BootstrapAdminCode) ? null : InviteCode.Normalize(BootstrapAdminCode);

    public bool HasStrongBootstrapCode() =>
        NormalizedBootstrapAdminCode is null || NormalizedBootstrapAdminCode.Length >= InviteCode.CodeLength;
}
