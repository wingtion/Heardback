using System.IO.Compression;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tokens;

namespace Heardback.Modules.Matching.Cvs.Extraction;

/// <summary>
/// PdfPig's Flate filter behind a size check: the stream is first inflated into a small scratch buffer that is
/// thrown away, only counting bytes. If the result would not fit the document's budget, decoding stops before
/// PdfPig allocates anything.
/// </summary>
internal sealed class BudgetedFlateFilter(IFilter inner, DecodeBudget budget) : IFilter
{
    private const int ScratchBufferSize = 81_920;
    private const int ZlibHeaderLength = 2;
    private const int ZlibCompressionMethodMask = 0x0F;
    private const int ZlibDeflateMethod = 8;

    public bool IsSupported => inner.IsSupported;

    public Memory<byte> Decode(
        Memory<byte> input,
        DictionaryToken streamDictionary,
        IFilterProvider filterProvider,
        int filterIndex)
    {
        budget.Spend(InflatedLength(input, budget.Remaining));
        return inner.Decode(input, streamDictionary, filterProvider, filterIndex);
    }

    /// <summary>Bytes the stream inflates to, counting no further than one past <paramref name="limit"/>.</summary>
    private static long InflatedLength(Memory<byte> input, long limit)
    {
        var deflated = HasZlibHeader(input.Span) ? input[ZlibHeaderLength..] : input;
        using var source = new MemoryStream(deflated.ToArray(), writable: false);
        using var inflater = new DeflateStream(source, CompressionMode.Decompress);
        var scratch = new byte[ScratchBufferSize];
        long total = 0;
        try
        {
            int read;
            while (total <= limit && (read = inflater.Read(scratch)) > 0)
            {
                total += read;
            }
        }
        catch (InvalidDataException)
        {
            // Damaged data: PdfPig decides how to handle it. What was readable so far still counts.
        }

        return total;
    }

    private static bool HasZlibHeader(ReadOnlySpan<byte> data) =>
        data.Length >= ZlibHeaderLength && (data[0] & ZlibCompressionMethodMask) == ZlibDeflateMethod;
}
