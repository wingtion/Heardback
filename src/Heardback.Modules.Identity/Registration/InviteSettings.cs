using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Identity.Domain;

namespace Heardback.Modules.Identity.Registration;

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

    /// <summary>
    /// Same shape as a generated invite code, so it can be typed into the registration form, which
    /// accepts exactly that. Separators and lower case are fine; they are normalized away.
    /// </summary>
    public bool HasValidBootstrapCode() =>
        BootstrapAdminCode is null
        || (BootstrapAdminCode.Length <= InviteCode.MaxInputLength
            && InviteCode.IsWellFormed(NormalizedBootstrapAdminCode ?? string.Empty));
}
