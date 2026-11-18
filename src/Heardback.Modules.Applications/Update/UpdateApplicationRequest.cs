using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Applications.Domain;
using Heardback.Modules.Postings.Contracts;

namespace Heardback.Modules.Applications.Update;

/// <summary>
/// The whole edit form. <see cref="Version"/> is the version the form was opened with; status is not here
/// because it changes through the board or Add update.
/// </summary>
internal sealed record UpdateApplicationRequest(
    [property: Required(ErrorMessage = ApplicationFieldMessages.CompanyRequired),
        MaxLength(PostingLimits.CompanyMaxLength)]
    string Company,
    [property: Required(ErrorMessage = ApplicationFieldMessages.RoleRequired), MaxLength(PostingLimits.TitleMaxLength)]
    string Role,
    [property: MaxLength(PostingLimits.UrlMaxLength), PostingUrl] string? Url,
    [property: MaxLength(PostingLimits.TextMaxLength)] string? PostingText,
    [property: Required] ApplicationChannel? Channel,
    [property: Required] DateOnly? AppliedOn,
    [property: MaxLength(Application.NotesMaxLength)] string? Notes,
    [property: Required] uint? Version)
{
    public ApplicationDetails ToDetails() => new(Company, Role, Url, PostingText, Channel!.Value, AppliedOn!.Value);
}
