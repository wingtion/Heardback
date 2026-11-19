using Heardback.Infrastructure.Llm.Persistence;
using Heardback.Infrastructure.Llm.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm.Quotas;

internal sealed class DailyCallBudget(
    LlmDbContext dbContext,
    IOptions<LlmSettings> options,
    TimeProvider timeProvider) : ILlmCallBudget
{
    public async Task<DateTimeOffset?> TryUseAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var day = LlmDay.Of(now);
        var limit = options.Value.MaxCallsPerDay;

        // The upsert only increments below the cap; a concurrent caller waits for the row lock and then sees the
        // new count, so the cap is never passed.
        var counted = await dbContext.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO llm.daily_call_counts AS counts (day, count) VALUES ({day}, 1)
            ON CONFLICT (day) DO UPDATE SET count = counts.count + 1 WHERE counts.count < {limit}
            """,
            cancellationToken);

        return counted == 1 ? null : LlmDay.NextReset(now);
    }
}
