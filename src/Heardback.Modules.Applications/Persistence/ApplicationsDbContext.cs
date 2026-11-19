using Heardback.Modules.Applications.Analysis;
using Heardback.Modules.Applications.Domain;
using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Persistence;

internal sealed class ApplicationsDbContext(DbContextOptions<ApplicationsDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext(options, currentUser)
{
    public const string Schema = "applications";

    public DbSet<Application> Applications => Set<Application>();

    public DbSet<ApplicationEvent> ApplicationEvents => Set<ApplicationEvent>();

    public DbSet<PostingAnalysis> PostingAnalyses => Set<PostingAnalysis>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationsDbContext).Assembly);
    }
}
