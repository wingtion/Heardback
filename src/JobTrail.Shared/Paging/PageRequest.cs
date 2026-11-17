using System.ComponentModel.DataAnnotations;

namespace JobTrail.Shared.Paging;

/// <summary>Page selection for list endpoints, bound from the query string (<c>?page=2&amp;pageSize=50</c>).</summary>
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public int Skip => (Page - 1) * PageSize;
}
