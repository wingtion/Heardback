using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobTrail.Shared.Persistence;

public static class ModuleDatabaseExtensions
{
    public const string MigrationsHistoryTable = "__ef_migrations_history";

    /// <summary>Registers and validates <see cref="DatabaseSettings"/>; the API fails to start without a connection string.</summary>
    public static IServiceCollection AddDatabaseSettings(this IServiceCollection services)
    {
        services.AddOptions<DatabaseSettings>()
            .BindConfiguration(DatabaseSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>Registers a module's DbContext on the shared database, inside the module's own schema.</summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string schema)
        where TContext : ModuleDbContext
    {
        services.AddDbContext<TContext>((provider, options) =>
        {
            var settings = provider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            options.UseModuleDatabase(settings.ConnectionString, schema);
        });
        services.AddScoped<IModuleDatabase, ModuleDatabase<TContext>>();

        return services;
    }

    /// <summary>
    /// Npgsql with snake_case names and a migrations history table per schema. Shared by runtime
    /// registration and each module's design-time factory so both produce the same model.
    /// </summary>
    public static DbContextOptionsBuilder UseModuleDatabase(
        this DbContextOptionsBuilder options,
        string connectionString,
        string schema) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable, schema))
            .UseSnakeCaseNamingConvention();

    /// <summary>Applies pending migrations for every registered module, when enabled in <see cref="DatabaseSettings"/>.</summary>
    public static async Task MigrateModuleDatabasesAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (!services.GetRequiredService<IOptions<DatabaseSettings>>().Value.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        foreach (var database in scope.ServiceProvider.GetServices<IModuleDatabase>())
        {
            await database.MigrateAsync(cancellationToken);
        }
    }
}
