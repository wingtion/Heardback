using System.ComponentModel.DataAnnotations;
using JobTrail.Modules.Identity.Domain;

namespace JobTrail.Modules.Identity.Registration;

internal sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(User.EmailMaxLength)] string Email,
    [property: Required, StringLength(PasswordPolicy.MaxLength, MinimumLength = PasswordPolicy.MinLength)]
    string Password,
    [property: Required, MaxLength(User.DisplayNameMaxLength)] string DisplayName,
    [property: Required] string InviteCode);
