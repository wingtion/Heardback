using Heardback.Infrastructure.Llm.Quotas;

namespace Heardback.UnitTests.Llm;

/// <summary>An app-wide call cap with a fixed number of calls left.</summary>
internal sealed class FakeCallBudget(int callsLeft = int.MaxValue) : ILlmCallBudget
{
    public static readonly DateTimeOffset ResetsAt = new(2026, 11, 20, 0, 0, 0, TimeSpan.Zero);

    public int Used { get; private set; }

    public Task<DateTimeOffset?> TryUseAsync(CancellationToken cancellationToken)
    {
        if (Used >= callsLeft)
        {
            return Task.FromResult<DateTimeOffset?>(ResetsAt);
        }

        Used++;
        return Task.FromResult<DateTimeOffset?>(null);
    }
}
