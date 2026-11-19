using Heardback.Shared.Persistence;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// The user's permission to send their CV text to the AI provider for tailoring suggestions (ADR-0008). Its row
/// existing means yes; withdrawing deletes it together with the suggestions made from the CV.
/// </summary>
internal sealed class AiConsent : IUserOwned
{
    private AiConsent()
    {
    }

    public Guid UserId { get; private set; }

    public DateTimeOffset GrantedAt { get; private set; }

    public static AiConsent Grant(DateTimeOffset now) => new() { GrantedAt = now };
}
