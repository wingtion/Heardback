using Heardback.Infrastructure.Llm;
using Heardback.Infrastructure.Llm.Jobs;
using Heardback.IntegrationTests.Api;
using Heardback.IntegrationTests.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.IntegrationTests.Llm;

/// <summary>The hosted worker end to end, on the real clock with a short poll interval.</summary>
[Collection(PostgresCollectionDefinition.Name)]
public sealed class AnalysisJobWorkerTests(PostgresFixture postgres)
{
    private const string Subject = "application:42";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Worker_JobQueuedWhileRunning_RunsIt()
    {
        var connectionString = await postgres.CreateDatabaseAsync(CancellationToken);
        var handler = new ScriptedJobHandler();
        await using (var factory = CreateFactory(connectionString, handler))
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var jobs = scope.ServiceProvider.GetRequiredService<IAnalysisJobs>();
            await jobs.EnqueueAsync(ScriptedJobHandler.TestKind, Subject, null, CancellationToken);

            var state = await WaitForAsync(
                () => jobs.GetLatestAsync(ScriptedJobHandler.TestKind, Subject, CancellationToken),
                latest => latest?.Status == AnalysisJobStatus.Succeeded);

            Assert.Equal(AnalysisJobStatus.Succeeded, state?.Status);
            Assert.Single(handler.Runs);
        }

        await postgres.DropDatabaseAsync(connectionString, CancellationToken);
    }

    private static ApiFactory CreateFactory(string connectionString, ScriptedJobHandler handler)
    {
        var factory = new ApiFactory
        {
            ConnectionString = connectionString,
            TestServices = services =>
            {
                services.AddSingleton<ILlmClient, EnabledLlmClient>();
                services.AddSingleton<IAnalysisJobHandler>(handler);
            },
        };
        factory.Settings["Llm:Jobs:PollInterval"] = "00:00:00.100";
        return factory;
    }

    private static async Task<T> WaitForAsync<T>(Func<Task<T>> read, Func<T, bool> done)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
        var value = await read();
        while (!done(value) && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken);
            value = await read();
        }

        return value;
    }
}
