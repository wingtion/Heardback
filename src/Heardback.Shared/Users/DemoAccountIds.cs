namespace Heardback.Shared.Users;

/// <summary>Demo account ids by kind.</summary>
/// <param name="Visitors">"Try the demo" sandboxes.</param>
/// <param name="Neighbors">The permanent made-up accounts behind the demo's community signals.</param>
public sealed record DemoAccountIds(IReadOnlyList<Guid> Visitors, IReadOnlyList<Guid> Neighbors)
{
    /// <summary>Every demo account, of either kind.</summary>
    public IReadOnlyList<Guid> All => [.. Visitors, .. Neighbors];
}
