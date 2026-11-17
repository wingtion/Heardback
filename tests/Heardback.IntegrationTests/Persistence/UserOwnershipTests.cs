using Heardback.IntegrationTests.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Heardback.IntegrationTests.Persistence;

[Collection(PostgresCollectionDefinition.Name)]
public sealed class UserOwnershipTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly Guid Alice = Guid.CreateVersion7();
    private static readonly Guid Bob = Guid.CreateVersion7();

    private string _connectionString = string.Empty;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _connectionString = await postgres.CreateDatabaseAsync(CancellationToken);
        await using var context = CreateContext(Alice);
        await context.Database.EnsureCreatedAsync(CancellationToken);
    }

    public async ValueTask DisposeAsync() => await postgres.DropDatabaseAsync(_connectionString, CancellationToken);

    [Fact]
    public async Task Query_TwoUsersWithData_ReturnsOnlyCurrentUsersRows()
    {
        await AddNoteAsync(Alice, "alice's note");
        await AddNoteAsync(Bob, "bob's note");

        await using var context = CreateContext(Bob);
        var texts = await context.Notes.Select(note => note.Text).ToListAsync(CancellationToken);

        Assert.Equal(["bob's note"], texts);
    }

    [Fact]
    public async Task FindAsync_OtherUsersRowById_ReturnsNull()
    {
        var aliceNoteId = await AddNoteAsync(Alice, "alice's note");

        await using var context = CreateContext(Bob);
        var note = await context.Notes.FindAsync([aliceNoteId], CancellationToken);

        Assert.Null(note);
    }

    [Fact]
    public async Task Query_AnonymousContext_ReturnsNoRows()
    {
        await AddNoteAsync(Alice, "alice's note");

        await using var context = CreateContext(userId: null);
        var count = await context.Notes.CountAsync(CancellationToken);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ExecuteDeleteAsync_AsOtherUser_DeletesNothing()
    {
        await AddNoteAsync(Alice, "alice's note");

        await using var bobContext = CreateContext(Bob);
        var deleted = await bobContext.Notes.ExecuteDeleteAsync(CancellationToken);

        Assert.Equal(0, deleted);
        await using var aliceContext = CreateContext(Alice);
        Assert.Equal(1, await aliceContext.Notes.CountAsync(CancellationToken));
    }

    [Fact]
    public async Task SaveChangesAsync_NewEntityWithoutOwner_StampsCurrentUser()
    {
        var noteId = await AddNoteAsync(Alice, "alice's note");

        await using var context = CreateContext(Alice);
        var note = await context.Notes.SingleAsync(CancellationToken);

        Assert.Equal(noteId, note.Id);
        Assert.Equal(Alice, note.UserId);
    }

    [Fact]
    public async Task SaveChangesAsync_NewEntityOwnedByOtherUser_Throws()
    {
        await using var context = CreateContext(Bob);
        context.Notes.Add(new TestNote("planted", userId: Alice));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(CancellationToken));
    }

    [Fact]
    public async Task SaveChangesAsync_NewEntityAsAnonymous_Throws()
    {
        await using var context = CreateContext(userId: null);
        context.Notes.Add(new TestNote("orphan"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(CancellationToken));
    }

    [Fact]
    public async Task SaveChangesAsync_ModifyOtherUsersRow_ThrowsAndLeavesRowUnchanged()
    {
        await AddNoteAsync(Alice, "alice's note");

        await using (var bobContext = CreateContext(Bob))
        {
            var aliceNote = await bobContext.Notes.IgnoreQueryFilters().SingleAsync(CancellationToken);
            aliceNote.Text = "tampered";

            await Assert.ThrowsAsync<InvalidOperationException>(() => bobContext.SaveChangesAsync(CancellationToken));
        }

        await using var aliceContext = CreateContext(Alice);
        Assert.Equal("alice's note", (await aliceContext.Notes.SingleAsync(CancellationToken)).Text);
    }

    [Fact]
    public async Task SaveChangesAsync_DeleteOtherUsersRow_Throws()
    {
        await AddNoteAsync(Alice, "alice's note");

        await using var bobContext = CreateContext(Bob);
        var aliceNote = await bobContext.Notes.IgnoreQueryFilters().SingleAsync(CancellationToken);
        bobContext.Notes.Remove(aliceNote);

        await Assert.ThrowsAsync<InvalidOperationException>(() => bobContext.SaveChangesAsync(CancellationToken));
    }

    private IsolationTestDbContext CreateContext(Guid? userId) =>
        new(
            new DbContextOptionsBuilder<IsolationTestDbContext>().UseNpgsql(_connectionString).Options,
            new TestCurrentUser(userId));

    private async Task<Guid> AddNoteAsync(Guid owner, string text)
    {
        await using var context = CreateContext(owner);
        var note = new TestNote(text);
        context.Notes.Add(note);
        await context.SaveChangesAsync(CancellationToken);
        return note.Id;
    }
}
