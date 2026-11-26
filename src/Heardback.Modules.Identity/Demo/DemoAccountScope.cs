using Heardback.Modules.Identity.Account;
using Heardback.Shared.Demo;
using Heardback.Shared.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Heardback.Modules.Identity.Demo;

/// <summary>
/// Runs work for one demo account in its own service scope whose current user is that account, so every module's
/// ownership rules apply exactly as in that user's own requests: seeded rows are stamped with the account, and the
/// erasers only see its rows.
/// </summary>
internal sealed class DemoAccountScope(IServiceScopeFactory scopeFactory)
{
    public Task SeedVisitorAsync(Guid userId, CancellationToken cancellationToken) =>
        RunAsAsync(userId, async services =>
        {
            foreach (var seeder in services.GetServices<IDemoDataSeeder>())
            {
                await seeder.SeedVisitorAsync(cancellationToken);
            }
        });

    public Task SeedNeighborAsync(Guid userId, int number, CancellationToken cancellationToken) =>
        RunAsAsync(userId, async services =>
        {
            foreach (var seeder in services.GetServices<IDemoDataSeeder>())
            {
                await seeder.SeedNeighborAsync(number, cancellationToken);
            }
        });

    public Task RemoveAsync(Guid userId, CancellationToken cancellationToken) =>
        RunAsAsync(userId, services => services.GetRequiredService<AccountRemover>().RemoveAsync(userId, cancellationToken));

    private async Task RunAsAsync(Guid userId, Func<IServiceProvider, Task> work)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<BackgroundUser>().Set(userId);
        await work(scope.ServiceProvider);
    }
}
