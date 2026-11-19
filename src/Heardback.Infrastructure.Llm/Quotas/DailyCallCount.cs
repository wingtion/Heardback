namespace Heardback.Infrastructure.Llm.Quotas;

/// <summary>LLM calls made by the whole app on one UTC day, capped below the providers' free limits.</summary>
internal sealed class DailyCallCount
{
    private DailyCallCount()
    {
    }

    public DateOnly Day { get; private set; }

    public int Count { get; private set; }
}
