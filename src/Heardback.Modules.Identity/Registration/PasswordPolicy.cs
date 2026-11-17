namespace Heardback.Modules.Identity.Registration;

/// <summary>Length-only policy, per NIST SP 800-63B: no composition rules, long passphrases allowed.</summary>
internal static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;
}
