using System.Collections.Concurrent;
using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Infrastructure.Llm.Persistence;
using Heardback.Infrastructure.Llm.Quotas;
using Heardback.IntegrationTests.Api;
using Heardback.IntegrationTests.Postgres;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Heardback.IntegrationTests.Llm;

/// <summary>The job queue and runner against real Postgres; the worker is off and each test drives the runner.</summary>
[Collection(PostgresCollectionDefinition.Name)]
public sealed class AnalysisJobsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Subject = "sha256:posting-text-hash";
    private const int MaxAttempts = 3;
    private const int DailyLimit = 3;
    private const int CallCap = 5;

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 19, 9, 0, 0, TimeSpan.Zero));
    private readonly ScriptedJobHandler _handler = new();
    private ApiFactory _factory = null!;
    private string _connectionString = string.Empty;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _connectionString = await postgres.CreateDatabaseAsync(CancellationToken);
        _factory = new ApiFactory
        {
            ConnectionString = _connectionString,
            Clock = _clock,
            TestServices = services =>
            {
                services.AddSingleton<ILlmClient, EnabledLlmClient>();
                services.AddScoped<IAnalysisJobHandler>(scope =>
                {
                    _handler.Services = scope;
                    return _handler;
                });
            },
        };
        _factory.Settings["Llm:Jobs:RunWorker"] = "false";
        _factory.Settings["Llm:Jobs:MaxAttempts"] = MaxAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _factory.Settings["Llm:Jobs:FirstRetryDelay"] = "00:00:30";
        _factory.Settings["Llm:MaxCallsPerDay"] = CallCap.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _ = _factory.Services;
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await postgres.DropDatabaseAsync(_connectionString, CancellationToken);
    }

    [Fact]
    public async Task EnqueueAsync_NewSubject_CreatesPendingJob()
    {
        var userId = Guid.NewGuid();

        var result = await EnqueueAsync(Subject, userId);

        Assert.Equal(EnqueueResult.Queued, result);
        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal(0, job.Attempts);
        Assert.Equal(_clock.GetUtcNow(), job.NextRunAt);
        Assert.Equal(userId, job.RequestedByUserId);
    }

    [Fact]
    public async Task EnqueueAsync_SameSubjectWhileUnfinished_IsAlreadyQueued()
    {
        await EnqueueAsync(Subject);

        var second = await EnqueueAsync(Subject);

        Assert.Equal(EnqueueResult.AlreadyQueued, second);
        Assert.Single(await ReadJobsAsync());
    }

    [Fact]
    public async Task EnqueueAsync_ManyConcurrentCalls_CreateOneJob()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => EnqueueAsync(Subject), CancellationToken)));

        Assert.Equal(1, results.Count(result => result == EnqueueResult.Queued));
        Assert.Single(await ReadJobsAsync());
    }

    [Fact]
    public async Task EnqueueAsync_AfterPreviousJobFinished_CreatesNewJob()
    {
        await EnqueueAsync(Subject);
        await RunNextAsync();

        var again = await EnqueueAsync(Subject);

        Assert.Equal(EnqueueResult.Queued, again);
        Assert.Equal(2, (await ReadJobsAsync()).Count);
    }

    [Fact]
    public async Task EnqueueAsync_LlmDisabled_QueuesNothing()
    {
        await using var factory = new ApiFactory { ConnectionString = _connectionString, Clock = _clock };
        await using var scope = factory.Services.CreateAsyncScope();

        var result = await scope.ServiceProvider.GetRequiredService<IAnalysisJobs>()
            .EnqueueAsync(ScriptedJobHandler.TestKind, Subject, null, CancellationToken);

        Assert.Equal(EnqueueResult.LlmDisabled, result);
        Assert.Empty(await ReadJobsAsync());
    }

    [Fact]
    public async Task GetLatestAsync_TwoJobsForSubject_ReturnsTheNewest()
    {
        await EnqueueAsync(Subject);
        await RunNextAsync();
        _clock.Advance(TimeSpan.FromMinutes(1));
        await EnqueueAsync(Subject);

        await using var scope = _factory.Services.CreateAsyncScope();
        var latest = await scope.ServiceProvider.GetRequiredService<IAnalysisJobs>()
            .GetLatestAsync(ScriptedJobHandler.TestKind, Subject, CancellationToken);

        Assert.NotNull(latest);
        Assert.Equal(AnalysisJobStatus.Pending, latest.Status);
    }

    [Fact]
    public async Task RunNextAsync_NoDueJob_ReturnsFalse()
    {
        Assert.False(await RunNextAsync());
    }

    [Fact]
    public async Task RunNextAsync_HandlerSucceeds_MarksJobSucceeded()
    {
        var userId = Guid.NewGuid();
        await EnqueueAsync(Subject, userId);

        Assert.True(await RunNextAsync());

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Succeeded, job.Status);
        Assert.Equal(1, job.Attempts);
        Assert.Null(job.LeaseToken);
        var run = Assert.Single(_handler.Runs);
        Assert.Equal(new AnalysisJobContext(job.Id, ScriptedJobHandler.TestKind, Subject, userId, 1), run);
    }

    [Fact]
    public async Task RunNextAsync_JobWithRequestingUser_HandlerSeesThatUserAsCurrentUser()
    {
        var userId = Guid.NewGuid();
        Guid? seen = Guid.Empty;
        bool? seenAdmin = null;
        _handler.Behaviour = (_, _) =>
        {
            var currentUser = _handler.Services!.GetRequiredService<ICurrentUser>();
            seen = currentUser.UserId;
            seenAdmin = currentUser.IsAdmin;
            return Task.FromResult(AnalysisJobOutcome.Succeeded());
        };
        await EnqueueAsync(Subject, userId);
        await EnqueueAsync(Subject + "-shared");

        await RunNextAsync();
        var forUser = seen;
        await RunNextAsync();

        Assert.Equal(userId, forUser);
        Assert.Null(seen);
        Assert.False(seenAdmin);
    }

    [Fact]
    public async Task RunNextAsync_RetryLater_SchedulesBackoffAndKeepsReason()
    {
        _handler.Behaviour = (_, _) => Task.FromResult(AnalysisJobOutcome.RetryLater("llm_unavailable"));
        await EnqueueAsync(Subject);

        await RunNextAsync();

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal("llm_unavailable", job.LastError);
        var wait = job.NextRunAt - _clock.GetUtcNow();
        Assert.InRange(wait, TimeSpan.FromSeconds(24), TimeSpan.FromSeconds(36));
        Assert.False(await RunNextAsync());
    }

    [Fact]
    public async Task RunNextAsync_RetryAfterLongerThanBackoff_WaitsForRetryAfter()
    {
        _handler.Behaviour = (_, _) =>
            Task.FromResult(AnalysisJobOutcome.RetryLater("llm_unavailable", TimeSpan.FromMinutes(10)));
        await EnqueueAsync(Subject);

        await RunNextAsync();

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(_clock.GetUtcNow().AddMinutes(10), job.NextRunAt);
    }

    [Fact]
    public async Task RunNextAsync_RetryLaterOnLastAttempt_MarksJobFailed()
    {
        _handler.Behaviour = (_, _) => Task.FromResult(AnalysisJobOutcome.RetryLater("llm_unavailable"));
        await EnqueueAsync(Subject);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            Assert.True(await RunNextAsync());
            _clock.Advance(TimeSpan.FromHours(1));
        }

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Failed, job.Status);
        Assert.Equal(MaxAttempts, job.Attempts);
        Assert.Equal("llm_unavailable", job.LastError);
        Assert.Equal([1, 2, 3], _handler.Runs.Select(run => run.Attempt));
        Assert.False(await RunNextAsync());
    }

    [Fact]
    public async Task RunNextAsync_Failed_GivesUpAtOnce()
    {
        _handler.Behaviour = (_, _) => Task.FromResult(AnalysisJobOutcome.Failed("invalid_output"));
        await EnqueueAsync(Subject);

        await RunNextAsync();

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Failed, job.Status);
        Assert.Equal("invalid_output", job.LastError);
    }

    [Fact]
    public async Task RunNextAsync_Deferred_WaitsWithoutUsingAnAttempt()
    {
        var tomorrow = _clock.GetUtcNow().Date.AddDays(1);
        _handler.Behaviour = (_, _) =>
            Task.FromResult(AnalysisJobOutcome.Deferred("quota_exhausted", new DateTimeOffset(tomorrow, TimeSpan.Zero)));
        await EnqueueAsync(Subject);

        await RunNextAsync();

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal(0, job.Attempts);
        Assert.Equal(new DateTimeOffset(tomorrow, TimeSpan.Zero), job.NextRunAt);
        Assert.Equal("quota_exhausted", job.LastError);
    }

    [Fact]
    public async Task RunNextAsync_HandlerThrows_RetriesLater()
    {
        _handler.Behaviour = (_, _) => throw new InvalidOperationException("handler bug");
        await EnqueueAsync(Subject);

        Assert.True(await RunNextAsync());

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal("handler_exception", job.LastError);
        Assert.Equal(1, job.Attempts);
    }

    [Fact]
    public async Task RunNextAsync_NoHandlerForKind_MarksJobFailed()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IAnalysisJobs>()
                .EnqueueAsync("unknown-kind", Subject, null, CancellationToken);
        }

        await RunNextAsync();

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Failed, job.Status);
        Assert.Equal("no_handler", job.LastError);
    }

    [Fact]
    public async Task RunNextAsync_ShutdownDuringRun_PutsJobBackWithoutUsingAnAttempt()
    {
        using var shutdown = new CancellationTokenSource();
        _handler.Behaviour = async (_, cancellationToken) =>
        {
            await shutdown.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return AnalysisJobOutcome.Succeeded();
        };
        await EnqueueAsync(Subject);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RunNextAsync(shutdown.Token));

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal(0, job.Attempts);
        Assert.Null(job.LeaseToken);
    }

    [Fact]
    public async Task RunNextAsync_LeaseExpired_ClaimsJobAgainAsNextAttempt()
    {
        await EnqueueAsync(Subject);
        await SimulateCrashedRunAsync(attempts: 1);

        Assert.True(await RunNextAsync());

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Succeeded, job.Status);
        Assert.Equal(2, Assert.Single(_handler.Runs).Attempt);
    }

    [Fact]
    public async Task RunNextAsync_LeaseExpiredAfterLastAttempt_MarksJobFailed()
    {
        await EnqueueAsync(Subject);
        await SimulateCrashedRunAsync(attempts: MaxAttempts);

        Assert.False(await RunNextAsync());

        var job = Assert.Single(await ReadJobsAsync());
        Assert.Equal(AnalysisJobStatus.Failed, job.Status);
        Assert.Equal("lease_expired", job.LastError);
        Assert.Empty(_handler.Runs);
    }

    [Fact]
    public async Task RunNextAsync_LeaseLiveOnAnotherWorker_IsNotClaimed()
    {
        await EnqueueAsync(Subject);
        await SimulateCrashedRunAsync(attempts: 1, leaseUntil: _clock.GetUtcNow().AddMinutes(1));

        Assert.False(await RunNextAsync());
        Assert.Empty(_handler.Runs);
    }

    [Fact]
    public async Task RunNextAsync_LeaseTakenOverDuringRun_DoesNotOverwriteNewerState()
    {
        _handler.Behaviour = async (job, cancellationToken) =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<LlmDbContext>().Database.ExecuteSqlAsync(
                $"UPDATE llm.analysis_jobs SET lease_token = {Guid.NewGuid()} WHERE id = {job.JobId}",
                cancellationToken);
            return AnalysisJobOutcome.Succeeded();
        };
        await EnqueueAsync(Subject);

        await RunNextAsync();

        Assert.Equal(AnalysisJobStatus.Running, Assert.Single(await ReadJobsAsync()).Status);
    }

    [Fact]
    public async Task RunNextAsync_TwoWorkersAtOnce_RunEveryJobExactlyOnce()
    {
        const int jobCount = 12;
        var running = new ConcurrentDictionary<Guid, int>();
        _handler.Behaviour = async (job, cancellationToken) =>
        {
            Assert.Equal(1, running.AddOrUpdate(job.JobId, 1, (_, count) => count + 1));
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            return AnalysisJobOutcome.Succeeded();
        };
        for (var index = 0; index < jobCount; index++)
        {
            await EnqueueAsync($"{Subject}-{index}");
        }

        await Task.WhenAll(
            Task.Run(DrainAsync, CancellationToken),
            Task.Run(DrainAsync, CancellationToken),
            Task.Run(DrainAsync, CancellationToken));

        Assert.Equal(jobCount, _handler.Runs.Count);
        Assert.Equal(jobCount, _handler.Runs.Select(run => run.JobId).Distinct().Count());
        Assert.All(await ReadJobsAsync(), job => Assert.Equal(AnalysisJobStatus.Succeeded, job.Status));

        async Task DrainAsync()
        {
            while (await RunNextAsync())
            {
            }
        }
    }

    [Fact]
    public async Task EnqueueForUserAsync_WithinLimit_QueuesAndCountsOneUse()
    {
        var userId = Guid.NewGuid();

        var result = await EnqueueForUserAsync(Subject, userId);

        Assert.Equal(EnqueueResult.Queued, result);
        var usage = await GetUsageAsync(userId);
        Assert.Equal(1, usage.Used);
        Assert.Equal(DailyLimit - 1, usage.Remaining);
        Assert.Equal(new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero), usage.ResetsAt);
        Assert.Equal(new DateOnly(2026, 11, 19), Assert.Single(await ReadJobsAsync()).QuotaDay);
    }

    [Fact]
    public async Task EnqueueForUserAsync_LimitUsedUp_QueuesAndCountsNothing()
    {
        var userId = Guid.NewGuid();
        for (var index = 0; index < DailyLimit; index++)
        {
            await EnqueueForUserAsync($"{Subject}-{index}", userId);
        }

        var result = await EnqueueForUserAsync($"{Subject}-over", userId);

        Assert.Equal(EnqueueResult.QuotaExceeded, result);
        Assert.Equal(DailyLimit, (await ReadJobsAsync()).Count);
        Assert.Equal(DailyLimit, (await GetUsageAsync(userId)).Used);
    }

    [Fact]
    public async Task EnqueueForUserAsync_AlreadyQueued_DoesNotCountAgain()
    {
        var userId = Guid.NewGuid();
        await EnqueueForUserAsync(Subject, userId);

        var second = await EnqueueForUserAsync(Subject, userId);

        Assert.Equal(EnqueueResult.AlreadyQueued, second);
        Assert.Equal(1, (await GetUsageAsync(userId)).Used);
    }

    [Fact]
    public async Task EnqueueForUserAsync_ManyAtOnce_NeverPassesTheLimit()
    {
        var userId = Guid.NewGuid();

        var results = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(index => Task.Run(() => EnqueueForUserAsync($"{Subject}-{index}", userId), CancellationToken)));

        Assert.Equal(DailyLimit, results.Count(result => result == EnqueueResult.Queued));
        Assert.Equal(DailyLimit, (await ReadJobsAsync()).Count);
        Assert.Equal(DailyLimit, (await GetUsageAsync(userId)).Used);
    }

    [Fact]
    public async Task EnqueueForUserAsync_LimitsArePerUser()
    {
        var alice = Guid.NewGuid();
        for (var index = 0; index < DailyLimit; index++)
        {
            await EnqueueForUserAsync($"{Subject}-{index}", alice);
        }

        var bobs = await EnqueueForUserAsync($"{Subject}-bob", Guid.NewGuid());

        Assert.Equal(EnqueueResult.Queued, bobs);
    }

    [Fact]
    public async Task EnqueueForUserAsync_NextUtcDay_StartsFromZero()
    {
        var userId = Guid.NewGuid();
        for (var index = 0; index < DailyLimit; index++)
        {
            await EnqueueForUserAsync($"{Subject}-{index}", userId);
        }

        _clock.Advance(TimeSpan.FromDays(1));
        var nextDay = await EnqueueForUserAsync($"{Subject}-next", userId);

        Assert.Equal(EnqueueResult.Queued, nextDay);
        Assert.Equal(1, (await GetUsageAsync(userId)).Used);
    }

    [Fact]
    public async Task RunNextAsync_QuotaJobFails_GivesTheUseBack()
    {
        var userId = Guid.NewGuid();
        _handler.Behaviour = (_, _) => Task.FromResult(AnalysisJobOutcome.Failed("invalid_output"));
        await EnqueueForUserAsync(Subject, userId);

        await RunNextAsync();

        Assert.Equal(0, (await GetUsageAsync(userId)).Used);
    }

    [Fact]
    public async Task RunNextAsync_QuotaJobRunsOutOfAttempts_GivesTheUseBack()
    {
        var userId = Guid.NewGuid();
        _handler.Behaviour = (_, _) => Task.FromResult(AnalysisJobOutcome.RetryLater("llm_unavailable"));
        await EnqueueForUserAsync(Subject, userId);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await RunNextAsync();
            Assert.Equal(attempt < MaxAttempts ? 1 : 0, (await GetUsageAsync(userId)).Used);
            _clock.Advance(TimeSpan.FromHours(1));
        }
    }

    [Fact]
    public async Task RunNextAsync_QuotaJobSucceeds_KeepsTheUse()
    {
        var userId = Guid.NewGuid();
        await EnqueueForUserAsync(Subject, userId);

        await RunNextAsync();

        Assert.Equal(1, (await GetUsageAsync(userId)).Used);
    }

    [Fact]
    public async Task RunNextAsync_QuotaJobLostItsLeaseTooOften_GivesTheUseBack()
    {
        var userId = Guid.NewGuid();
        await EnqueueForUserAsync(Subject, userId);
        await EnqueueForUserAsync(Subject + "-2", userId);
        await SimulateCrashedRunAsync(attempts: MaxAttempts);

        await RunNextAsync();

        Assert.All(await ReadJobsAsync(), job => Assert.Equal("lease_expired", job.LastError));
        Assert.Equal(0, (await GetUsageAsync(userId)).Used);
    }

    [Fact]
    public async Task TryUseAsync_CapReached_RefusesAndSaysWhenItResets()
    {
        for (var call = 0; call < CallCap; call++)
        {
            Assert.Null(await TryUseCallAsync());
        }

        Assert.Equal(new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero), await TryUseCallAsync());
        _clock.Advance(TimeSpan.FromDays(1));
        Assert.Null(await TryUseCallAsync());
    }

    [Fact]
    public async Task TryUseAsync_ManyAtOnce_NeverPassesTheCap()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => Task.Run(TryUseCallAsync, CancellationToken)));

        Assert.Equal(CallCap, results.Count(resetsAt => resetsAt is null));
    }

    private async Task<EnqueueResult> EnqueueForUserAsync(string subjectKey, Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAnalysisJobs>()
            .EnqueueForUserAsync(ScriptedJobHandler.TestKind, subjectKey, userId, DailyLimit, CancellationToken);
    }

    private async Task<LlmUsage> GetUsageAsync(Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAnalysisJobs>()
            .GetUsageAsync(ScriptedJobHandler.TestKind, userId, DailyLimit, CancellationToken);
    }

    private async Task<DateTimeOffset?> TryUseCallAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ILlmCallBudget>().TryUseAsync(CancellationToken);
    }

    private async Task<EnqueueResult> EnqueueAsync(string subjectKey, Guid? userId = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IAnalysisJobs>()
            .EnqueueAsync(ScriptedJobHandler.TestKind, subjectKey, userId, CancellationToken);
    }

    private async Task<bool> RunNextAsync(CancellationToken? cancellationToken = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AnalysisJobRunner>()
            .RunNextAsync(cancellationToken ?? CancellationToken);
    }

    /// <summary>Leaves the job as a worker that claimed it and then died would: Running, with a lease.</summary>
    private async Task SimulateCrashedRunAsync(int attempts, DateTimeOffset? leaseUntil = null)
    {
        var until = leaseUntil ?? _clock.GetUtcNow().AddMinutes(-1);
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<LlmDbContext>().Database.ExecuteSqlAsync(
            $"""
            UPDATE llm.analysis_jobs
            SET status = 'Running', attempts = {attempts}, lease_token = {Guid.NewGuid()}, lease_until = {until}
            """,
            CancellationToken);
    }

    private async Task<List<AnalysisJob>> ReadJobsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<LlmDbContext>().AnalysisJobs
            .AsNoTracking()
            .OrderBy(job => job.CreatedAt)
            .ThenBy(job => job.Id)
            .ToListAsync(CancellationToken);
    }
}
