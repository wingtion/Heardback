namespace Heardback.Modules.Matching.Cvs.Extraction;

/// <summary>
/// How many decompressed bytes one PDF may still produce. Shared by every stream of the document, so many
/// medium streams cannot add up to a bomb either.
/// </summary>
internal sealed class DecodeBudget(long maxBytes)
{
    private long _remaining = maxBytes;

    /// <summary>Bytes still allowed.</summary>
    public long Remaining => _remaining;

    /// <summary>Set once a stream would have gone over the budget; the document is then rejected.</summary>
    public bool Exceeded { get; private set; }

    /// <summary>Takes <paramref name="bytes"/> from the budget, or marks it exceeded and throws.</summary>
    public void Spend(long bytes)
    {
        if (Exceeded || bytes > _remaining)
        {
            Exceeded = true;
            throw new DecodeBudgetExceededException();
        }

        _remaining -= bytes;
    }

    /// <summary>Rejects the document outright, e.g. for a compression filter that is not unpacked at all.</summary>
    public void Refuse()
    {
        Exceeded = true;
        throw new DecodeBudgetExceededException();
    }
}
