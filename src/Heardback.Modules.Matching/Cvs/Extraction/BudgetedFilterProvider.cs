using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tokens;

namespace Heardback.Modules.Matching.Cvs.Extraction;

/// <summary>
/// PdfPig's filters with a decompression budget for one document (ADR-0006). Flate, which nearly every CV
/// uses, is measured before it unpacks. LZW and run-length can expand a few kilobytes into hundreds of
/// megabytes and are not used by today's PDF exporters, so they are refused. Image filters stay as they are:
/// reading text never decodes images.
/// </summary>
internal sealed class BudgetedFilterProvider(DecodeBudget budget) : IFilterProvider
{
    private readonly IFilterProvider _inner = DefaultFilterProvider.Instance;

    public IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary) => Wrap(_inner.GetFilters(dictionary));

    public IReadOnlyList<IFilter> GetNamedFilters(IReadOnlyList<NameToken> names) =>
        Wrap(_inner.GetNamedFilters(names));

    public IReadOnlyList<IFilter> GetAllFilters() => Wrap(_inner.GetAllFilters());

    private List<IFilter> Wrap(IReadOnlyList<IFilter> filters) =>
        filters.Select(filter => filter switch
        {
            FlateFilter => new BudgetedFlateFilter(filter, budget),
            LzwFilter or RunLengthFilter => new RefusedFilter(budget),
            _ => filter,
        }).ToList();
}
