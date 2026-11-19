using Heardback.Infrastructure.Llm.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm.Jobs;

/// <summary>Runs due jobs one after another, then waits for the poll interval. Idle while the LLM is disabled.</summary>
internal sealed partial class AnalysisJobWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AnalysisJobSettings> options,
    TimeProvider timeProvider,
    ILogger<AnalysisJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.RunWorker || !IsLlmEnabled())
        {
            LogIdle(settings.RunWorker);
            return;
        }

        using var timer = new PeriodicTimer(settings.PollInterval, timeProvider);
        try
        {
            do
            {
                await RunDueJobsAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private bool IsLlmEnabled()
    {
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ILlmClient>().IsEnabled;
    }

    private async Task RunDueJobsAsync(CancellationToken stoppingToken)
    {
        try
        {
            bool ranJob;
            do
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                ranJob = await scope.ServiceProvider.GetRequiredService<AnalysisJobRunner>().RunNextAsync(stoppingToken);
            }
            while (ranJob && !stoppingToken.IsCancellationRequested);
        }
#pragma warning disable CA1031 // A database hiccup must not stop the worker; it tries again on the next tick.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogPassFailed(exception.GetType().Name);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Analysis job worker idle (worker enabled: {RunWorker}, LLM disabled or worker off)")]
    private partial void LogIdle(bool runWorker);

    [LoggerMessage(Level = LogLevel.Error, Message = "Analysis job pass failed with {ExceptionType}; retrying on the next tick")]
    private partial void LogPassFailed(string exceptionType);
}
