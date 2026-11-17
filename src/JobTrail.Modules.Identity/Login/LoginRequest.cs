using System.ComponentModel.DataAnnotations;
using JobTrail.Modules.Identity.Domain;
using JobTrail.Modules.Identity.Registration;

namespace JobTrail.Modules.Identity.Login;

internal sealed record LoginRequest(
    [property: Required, MaxLength(User.EmailMaxLength)] string Email,
    [property: Required, MaxLength(PasswordPolicy.MaxLength)] string Password);
