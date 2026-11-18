using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Postings.Contracts;

namespace Heardback.Modules.Applications.Create;

internal sealed record CreateApplicationRequest(
    [property: Required(ErrorMessage = ApplicationFieldMessages.CompanyRequired),
        MaxLength(PostingLimits.CompanyMaxLength)]
    string Company,
    [property: Required(ErrorMessage = ApplicationFieldMessages.RoleRequired), MaxLength(PostingLimits.TitleMaxLength)]
    string Role,
    [property: MaxLength(PostingLimits.UrlMaxLength), PostingUrl] string? Url,
    [property: MaxLength(PostingLimits.TextMaxLength)] string? PostingText,
    [property: Required] ApplicationChannel? Channel,
    [property: Required] DateOnly? AppliedOn)
{
    public ApplicationDetails ToDetails() => new(Company, Role, Url, PostingText, Channel!.Value, AppliedOn!.Value);
}
