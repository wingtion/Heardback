using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Infrastructure.Llm.Quotas;
using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Infrastructure.Llm.Persistence;

internal sealed class LlmDbContext(DbContextOptions<LlmDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext(options, currentUser)
{
    public const string Schema = "llm";

    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();

    public DbSet<DailyUsage> DailyUsage => Set<DailyUsage>();

    public DbSet<DailyCallCount> DailyCallCounts => Set<DailyCallCount>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LlmDbContext).Assembly);
    }
}
