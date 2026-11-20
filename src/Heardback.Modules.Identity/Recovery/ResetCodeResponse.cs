namespace Heardback.Modules.Identity.Recovery;

/// <summary>A newly issued reset code. It is shown once; only its hash is stored.</summary>
internal sealed record ResetCodeResponse(string Code, DateTimeOffset ExpiresAt);
