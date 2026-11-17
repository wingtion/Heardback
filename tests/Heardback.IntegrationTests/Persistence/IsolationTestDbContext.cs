using Heardback.Shared.Persistence;
using Heardback.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Heardback.IntegrationTests.Persistence;

/// <summary>A minimal module-style context used to prove the shared ownership rules against real Postgres.</summary>
internal sealed class IsolationTestDbContext(DbContextOptions<IsolationTestDbContext> options, ICurrentUser currentUser)
    : ModuleDbContext(options, currentUser)
{
    public DbSet<TestNote> Notes => Set<TestNote>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("isolation_test");
        modelBuilder.Entity<TestNote>().Property(note => note.Text).HasMaxLength(200);
    }
}

internal sealed class TestNote : IUserOwned
{
    public TestNote(string text, Guid userId = default)
    {
        Text = text;
        UserId = userId;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid UserId { get; private set; }

    public string Text { get; set; }
}
