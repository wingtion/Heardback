using Heardback.Modules.Identity.Domain;

namespace Heardback.Modules.Identity.CurrentUser;

/// <param name="Id">The user's id.</param>
/// <param name="Email">The user's email; a made-up address for demo visitors.</param>
/// <param name="DisplayName">The name shown in the app.</param>
/// <param name="IsAdmin">Whether the user can open the admin pages.</param>
/// <param name="IsDemo">A "Try the demo" visitor (ADR-0018).</param>
/// <param name="DemoEndsAt">When a demo visitor's account and data are removed; null for everyone else.</param>
internal sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsAdmin,
    bool IsDemo,
    DateTimeOffset? DemoEndsAt)
{
    public static CurrentUserResponse From(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.IsAdmin, user.Kind == UserKind.DemoVisitor, user.ExpiresAt);
}
