using Heardback.Infrastructure.Llm.Jobs;
using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Infrastructure.Llm.Persistence;

internal sealed class LlmDbContext(DbContextOptions<LlmDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext(options, currentUser)
{
    public const string Schema = "llm";

    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LlmDbContext).Assembly);
    }
}
