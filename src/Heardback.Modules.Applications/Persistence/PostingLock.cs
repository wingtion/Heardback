using Microsoft.EntityFrameworkCore;

namespace Heardback.Modules.Applications.Persistence;

/// <summary>
/// A Postgres transaction-level advisory lock per shared posting. Linking an application to a posting and deleting
/// a posting nobody uses both take it, so a posting cannot be deleted while an application is being linked to it
/// (ADR-0015). It is released when the surrounding transaction ends.
/// </summary>
internal static class PostingLock
{
    // Separates these locks from any other advisory lock the app may take later.
    private const int PostingLockSpace = 0x4842_5031;

    public static async Task LockPostingAsync(
        this ApplicationsDbContext dbContext,
        Guid postingId,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("The posting lock must be taken inside a transaction.");
        }

        var key = KeyOf(postingId);
        await dbContext.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock({PostingLockSpace}, {key})",
            cancellationToken);
    }

    // Computed from the id's bytes rather than GetHashCode, so every API instance derives the same key. Two postings
    // sharing a key only makes one wait for the other; it never lets a delete through.
    private static int KeyOf(Guid postingId)
    {
        Span<byte> bytes = stackalloc byte[16];
        postingId.TryWriteBytes(bytes);
        var parts = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, int>(bytes);
        return parts[0] ^ parts[1] ^ parts[2] ^ parts[3];
    }
}
