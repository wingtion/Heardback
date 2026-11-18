using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tokens;

namespace Heardback.Modules.Matching.Cvs.Extraction;

/// <summary>A compression filter the CV reader does not unpack; using it rejects the whole PDF.</summary>
internal sealed class RefusedFilter(DecodeBudget budget) : IFilter
{
    public bool IsSupported => true;

    public Memory<byte> Decode(
        Memory<byte> input,
        DictionaryToken streamDictionary,
        IFilterProvider filterProvider,
        int filterIndex)
    {
        budget.Refuse();
        return Memory<byte>.Empty;
    }
}
