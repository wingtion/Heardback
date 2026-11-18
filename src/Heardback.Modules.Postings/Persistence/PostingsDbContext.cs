using Heardback.Modules.Postings.Domain;
using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Postings.Persistence;

internal sealed class PostingsDbContext(DbContextOptions<PostingsDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext(options, currentUser)
{
    public const string Schema = "postings";

    public DbSet<JobPosting> JobPostings => Set<JobPosting>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PostingsDbContext).Assembly);
    }
}
