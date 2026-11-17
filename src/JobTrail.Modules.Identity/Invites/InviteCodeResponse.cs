using JobTrail.Modules.Identity.Domain;

namespace JobTrail.Modules.Identity.Invites;

internal sealed record InviteCodeResponse(
    Guid Id,
    string Code,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    InviteCodeStatus Status,
    DateTimeOffset? UsedAt);
