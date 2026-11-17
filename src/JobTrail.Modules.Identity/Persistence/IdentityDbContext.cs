using JobTrail.Modules.Identity.Domain;
using JobTrail.Shared.Persistence;
using JobTrail.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace JobTrail.Modules.Identity.Persistence;

internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext(options, currentUser)
{
    public const string Schema = "identity";

    public DbSet<User> Users => Set<User>();

    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}
