using JobTrail.Shared.Results;

namespace JobTrail.Modules.Identity;

internal static class IdentityErrors
{
    // Deliberately identical for unknown email and wrong password, so it does not reveal who has an account.
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("auth.invalid_credentials", "Email or password is incorrect.");

    public static readonly Error InvalidInviteCode =
        Error.Validation("auth.invalid_invite_code", "inviteCode", "This invite code is invalid, expired or already used.");

    public static readonly Error EmailTaken =
        Error.Conflict("auth.email_taken", "An account with this email already exists.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("auth.invalid_refresh_token", "Your session has expired. Please sign in again.");

    public static readonly Error UserNotFound =
        Error.Unauthorized("auth.user_not_found", "Your account no longer exists. Please sign in again.");
}
