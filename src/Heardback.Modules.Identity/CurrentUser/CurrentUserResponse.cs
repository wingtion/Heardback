using Heardback.Modules.Identity.Domain;

namespace Heardback.Modules.Identity.CurrentUser;

internal sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, bool IsAdmin)
{
    public static CurrentUserResponse From(User user) => new(user.Id, user.Email, user.DisplayName, user.IsAdmin);
}
