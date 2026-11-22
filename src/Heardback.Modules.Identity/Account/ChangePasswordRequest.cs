using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Identity.Registration;

namespace Heardback.Modules.Identity.Account;

internal sealed record ChangePasswordRequest(
    [property: Required, MaxLength(PasswordPolicy.MaxLength)] string CurrentPassword,
    [property: Required, StringLength(PasswordPolicy.MaxLength, MinimumLength = PasswordPolicy.MinLength)]
    string NewPassword);
