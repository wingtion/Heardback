namespace Heardback.Shared.Demo;

/// <summary>The permanent made-up accounts behind the community signals demo visitors see (ADR-0018).</summary>
public static class DemoNeighbors
{
    /// <summary>
    /// Enough to pass the community minimum of three other members on some postings and stay under it on others,
    /// so a visitor sees both a signal and the "not enough data yet" state.
    /// </summary>
    public const int Count = 4;
}
