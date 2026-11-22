using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Registration;

namespace Heardback.Modules.Identity.Recovery;

internal sealed record ResetPasswordRequest(
    [property: Required, MaxLength(InviteCode.MaxInputLength)] string Code,
    [property: Required, StringLength(PasswordPolicy.MaxLength, MinimumLength = PasswordPolicy.MinLength)]
    string NewPassword);
