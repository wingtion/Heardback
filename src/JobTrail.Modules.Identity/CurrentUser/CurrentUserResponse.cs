using JobTrail.Modules.Identity.Domain;

namespace JobTrail.Modules.Identity.CurrentUser;

internal sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, bool IsAdmin)
{
    public static CurrentUserResponse From(User user) => new(user.Id, user.Email, user.DisplayName, user.IsAdmin);
}
