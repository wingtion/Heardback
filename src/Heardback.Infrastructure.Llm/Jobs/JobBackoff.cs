namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>Exponential backoff with jitter, so jobs that failed together do not all retry at the same moment.</summary>
internal static class JobBackoff
{
    private const double JitterRange = 0.4;

    /// <param name="attempt">The attempt that just failed, starting at 1.</param>
    /// <param name="first">Wait after the first failure.</param>
    /// <param name="max">Longest wait.</param>
    /// <param name="jitterSample">A number in [0, 1); scales the wait by 0.8 to 1.2.</param>
    /// <param name="retryAfter">The provider's requested wait; the result is never shorter.</param>
    public static TimeSpan Delay(int attempt, TimeSpan first, TimeSpan max, double jitterSample, TimeSpan? retryAfter)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        var exponent = Math.Min(attempt - 1, 30);
        var baseTicks = Math.Min(first.Ticks * Math.Pow(2, exponent), max.Ticks);
        var jittered = TimeSpan.FromTicks((long)Math.Min(baseTicks * (1 - (JitterRange / 2) + (JitterRange * jitterSample)), max.Ticks));

        return retryAfter is { } wait && wait > jittered ? wait : jittered;
    }
}
