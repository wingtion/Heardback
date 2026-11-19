namespace Heardback.Infrastructure.Llm.Quotas;

/// <summary>Daily limits count per UTC day and reset at midnight UTC (decided 2026-11-19, ADR-0008).</summary>
internal static class LlmDay
{
    public static DateOnly Of(DateTimeOffset time) => DateOnly.FromDateTime(time.UtcDateTime);

    public static DateTimeOffset NextReset(DateTimeOffset now) =>
        new(Of(now).AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
