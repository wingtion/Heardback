using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Postings.Contracts;

namespace Heardback.Modules.Applications.Read;

/// <summary>Query string filters shared by the list and the board (<c>?search=&amp;status=&amp;channel=</c>).</summary>
internal sealed record ApplicationFilter(
    [property: MaxLength(PostingLimits.CompanyMaxLength)] string? Search = null,
    [property: EnumDataType(typeof(ApplicationListStatus))] ApplicationListStatus? Status = null,
    [property: EnumDataType(typeof(ApplicationChannel))] ApplicationChannel? Channel = null);
