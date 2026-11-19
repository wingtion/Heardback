namespace Heardback.Modules.Matching.Tailoring;

/// <param name="Allowed">Whether CV text may be sent to the AI provider for tailoring suggestions.</param>
/// <param name="AllowedAt">When the user allowed it.</param>
/// <param name="Available">Whether AI features are on (a provider is configured); the UI hides them otherwise.</param>
internal sealed record AiConsentResponse(bool Allowed, DateTimeOffset? AllowedAt, bool Available);
