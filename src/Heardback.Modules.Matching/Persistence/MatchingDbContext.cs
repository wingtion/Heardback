using Heardback.Modules.Matching.Cvs.Domain;
using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Matching.Persistence;

internal sealed class MatchingDbContext(DbContextOptions<MatchingDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext(options, currentUser)
{
    public const string Schema = "matching";

    public DbSet<Cv> Cvs => Set<Cv>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatchingDbContext).Assembly);
    }
}
