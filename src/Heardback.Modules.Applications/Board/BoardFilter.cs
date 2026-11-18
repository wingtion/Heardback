using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Applications.Read;
using Heardback.Modules.Postings.Contracts;

namespace Heardback.Modules.Applications.Board;

/// <summary>The board's toolbar: search and channel. There is no status filter, the columns are the statuses.</summary>
internal sealed record BoardFilter(
    [property: MaxLength(PostingLimits.CompanyMaxLength)] string? Search = null,
    ApplicationChannel? Channel = null)
{
    public ApplicationFilter ToApplicationFilter() => new(Search, Status: null, Channel);
}
