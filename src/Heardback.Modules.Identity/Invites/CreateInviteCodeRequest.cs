using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Identity.Invites;

/// <summary>Optional body; without it the code gets the configured default lifetime.</summary>
internal sealed record CreateInviteCodeRequest(
    [property: Range(CreateInviteCodeRequest.MinExpiryDays, CreateInviteCodeRequest.MaxExpiryDays)] int? ExpiresInDays)
{
    public const int MinExpiryDays = 1;
    public const int MaxExpiryDays = 90;
}
