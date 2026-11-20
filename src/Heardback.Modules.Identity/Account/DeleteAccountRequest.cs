using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Identity.Registration;

namespace Heardback.Modules.Identity.Account;

/// <summary>The current password, so a stolen access token or a slip cannot delete an account (ADR-0015).</summary>
internal sealed record DeleteAccountRequest([property: Required, MaxLength(PasswordPolicy.MaxLength)] string Password);
