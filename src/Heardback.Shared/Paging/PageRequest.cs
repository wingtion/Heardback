using System.ComponentModel.DataAnnotations;

namespace Heardback.Shared.Paging;

/// <summary>
/// Page selection for list endpoints, bound with <c>[AsParameters]</c> from the query string
/// (<c>?page=2&amp;pageSize=50</c>). Constructor defaults make both parameters optional.
/// </summary>
public sealed record PageRequest(
    [property: Range(1, int.MaxValue)] int Page = 1,
    [property: Range(1, PageRequest.MaxPageSize)] int PageSize = PageRequest.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;
}
