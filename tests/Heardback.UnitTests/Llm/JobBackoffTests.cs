using Heardback.Infrastructure.Llm.Jobs;

namespace Heardback.UnitTests.Llm;

public sealed class JobBackoffTests
{
    private static readonly TimeSpan First = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Max = TimeSpan.FromHours(1);

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(5, 480)]
    public void Delay_MiddleJitter_DoublesWithEachAttempt(int attempt, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), JobBackoff.Delay(attempt, First, Max, 0.5, null));
    }

    [Theory]
    [InlineData(0.0, 24)]
    [InlineData(0.999999, 36)]
    public void Delay_Jitter_StaysWithinTwentyPercent(double sample, int expectedSeconds)
    {
        var delay = JobBackoff.Delay(1, First, Max, sample, null);

        Assert.Equal(expectedSeconds, delay.TotalSeconds, precision: 0);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(40)]
    [InlineData(int.MaxValue)]
    public void Delay_ManyAttempts_IsCappedAtMax(int attempt)
    {
        Assert.Equal(Max, JobBackoff.Delay(attempt, First, Max, 0.999999, null));
    }

    [Fact]
    public void Delay_RetryAfterLonger_UsesRetryAfter()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), JobBackoff.Delay(1, First, Max, 0.5, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Delay_RetryAfterShorter_KeepsBackoff()
    {
        Assert.Equal(First, JobBackoff.Delay(1, First, Max, 0.5, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void Delay_AttemptZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => JobBackoff.Delay(0, First, Max, 0.5, null));
    }
}
