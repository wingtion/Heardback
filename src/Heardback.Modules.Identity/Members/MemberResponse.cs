namespace Heardback.Modules.Identity.Members;

/// <summary>
/// One member as an admin sees them, so they can tell a member their email or give them a reset code (ADR-0015).
/// <see cref="ResetCodeExpiresAt"/> is when the member's open reset code expires, or null when there is none.
/// </summary>
internal sealed record MemberResponse(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsAdmin,
    DateTimeOffset JoinedAt,
    DateTimeOffset? ResetCodeExpiresAt);
