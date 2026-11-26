using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heardback.Modules.Identity.Demo;

/// <summary>
/// When the demo is on: creates the demo neighbors at startup, then removes expired demo visitors at startup and every
/// <see cref="DemoSettings.CleanupInterval"/>. The machine sleeps when idle, so a removal can come later than the
/// expiry; an expired visitor can no longer refresh their session in the meantime, and nobody else sees their data.
/// </summary>
internal sealed partial class DemoMaintenanceService(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoSettings> settings,
    TimeProvider timeProvider,
    ILogger<DemoMaintenanceService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Value.Enabled)
        {
            return;
        }

        await RunSafelyAsync(EnsureNeighborsAsync, stoppingToken);

        using var timer = new PeriodicTimer(settings.Value.CleanupInterval, timeProvider);
        do
        {
            await RunSafelyAsync(RemoveExpiredAsync, stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EnsureNeighborsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoNeighborProvisioner>().EnsureAsync(cancellationToken);
    }

    private async Task RemoveExpiredAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoAccountCleaner>().RemoveExpiredAsync(cancellationToken);
    }

    // A failed round must not stop the service; the next tick tries again.
    private async Task RunSafelyAsync(Func<CancellationToken, Task> work, CancellationToken stoppingToken)
    {
        try
        {
            await work(stoppingToken);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            LogFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Demo maintenance failed; it runs again on the next round")]
    private partial void LogFailed(Exception exception);
}
