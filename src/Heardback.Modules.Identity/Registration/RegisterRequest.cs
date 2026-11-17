using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Identity.Domain;

namespace Heardback.Modules.Identity.Registration;

internal sealed record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(User.EmailMaxLength)] string Email,
    [property: Required, StringLength(PasswordPolicy.MaxLength, MinimumLength = PasswordPolicy.MinLength)]
    string Password,
    [property: Required, MaxLength(User.DisplayNameMaxLength)] string DisplayName,
    [property: Required, MaxLength(Domain.InviteCode.MaxInputLength)] string InviteCode);
