using Heardback.Shared.Results;

namespace Heardback.Modules.Identity;

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

    // Another request (e.g. a second tab) refreshed this session a moment ago. The browser's cookie already
    // holds the newer token, so the client should simply retry; the cookie must not be cleared.
    public static readonly Error RefreshSuperseded =
        Error.Conflict("auth.refresh_superseded", "This session was just refreshed by another request. Retry the refresh.");

    public static readonly Error WrongPassword =
        Error.Validation("account.wrong_password", "password", "This password is incorrect.");

    public static readonly Error WrongCurrentPassword =
        Error.Validation("account.wrong_password", "currentPassword", "This password is incorrect.");

    // Deliberately one message for unknown, expired and used codes.
    public static readonly Error InvalidResetCode =
        Error.Validation("auth.invalid_reset_code", "code", "This reset code is invalid, expired or already used.");

    public static readonly Error MemberNotFound =
        Error.NotFound("admin.member_not_found", "This member does not exist.");

    public static readonly Error AdminResetNotAllowed =
        Error.Forbidden("admin.reset_admin", "Admins reset their password with the owner command on the server.");

    public static readonly Error LastAdmin =
        Error.Conflict("account.last_admin", "You are the only admin, so this account cannot be deleted.");

    public static readonly Error UserNotFound =
        Error.Unauthorized("auth.user_not_found", "Your account no longer exists. Please sign in again.");
}
