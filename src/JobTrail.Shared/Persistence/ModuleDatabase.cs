using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobTrail.Shared.Persistence;

internal sealed partial class ModuleDatabase<TContext>(TContext context, ILogger<ModuleDatabase<TContext>> logger)
    : IModuleDatabase
    where TContext : ModuleDbContext
{
    public string Name => typeof(TContext).Name;

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        LogMigrating(Name);
        await context.Database.MigrateAsync(cancellationToken);
    }

    public bool HasPendingModelChanges() => context.Database.HasPendingModelChanges();

    [LoggerMessage(Level = LogLevel.Information, Message = "Applying pending migrations for {DbContext}")]
    private partial void LogMigrating(string dbContext);
}
