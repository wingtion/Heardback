namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>A job row returned by the claim query.</summary>
internal sealed class ClaimedJob
{
    public Guid Id { get; init; }

    public string Kind { get; init; } = string.Empty;

    public string SubjectKey { get; init; } = string.Empty;

    public Guid? RequestedByUserId { get; init; }

    /// <summary>Including the run that is starting now.</summary>
    public int Attempts { get; init; }

    public Guid LeaseToken { get; init; }
}
