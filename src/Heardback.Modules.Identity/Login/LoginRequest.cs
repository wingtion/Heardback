using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Registration;

namespace Heardback.Modules.Identity.Login;

internal sealed record LoginRequest(
    [property: Required, MaxLength(User.EmailMaxLength)] string Email,
    [property: Required, MaxLength(PasswordPolicy.MaxLength)] string Password);
