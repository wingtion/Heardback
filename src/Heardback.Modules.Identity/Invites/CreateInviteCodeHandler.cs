using Heardback.Modules.Identity.Domain;
using Heardback.Modules.Identity.Persistence;
using Heardback.Modules.Identity.Registration;
using Heardback.Shared.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Identity.Invites;

internal sealed partial class CreateInviteCodeHandler(
    IdentityDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<InviteSettings> settings,
    TimeProvider timeProvider,
    ILogger<CreateInviteCodeHandler> logger)
{
    public async Task<InviteCodeResponse> HandleAsync(CreateInviteCodeRequest? request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var lifetime = request?.ExpiresInDays is { } days ? TimeSpan.FromDays(days) : settings.Value.DefaultLifetime;
        var invite = InviteCode.Generate(currentUser.GetRequiredUserId(), now, lifetime);

        dbContext.InviteCodes.Add(invite);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogCreated(invite.Id, invite.CreatedByUserId);
        return new InviteCodeResponse(
            invite.Id,
            invite.DisplayCode,
            invite.CreatedAt,
            invite.ExpiresAt,
            invite.GetStatus(now),
            invite.UsedAt);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Invite code {InviteCodeId} created by {UserId}")]
    private partial void LogCreated(Guid inviteCodeId, Guid userId);
}
