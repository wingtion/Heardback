using System.ComponentModel.DataAnnotations;
using Heardback.Modules.Postings.Contracts;

namespace Heardback.Modules.Applications.Read;

/// <summary><c>?url=</c> for "do I already track this posting?".</summary>
internal sealed record TrackedApplicationQuery(
    [property: Required, MaxLength(PostingLimits.UrlMaxLength), PostingUrl] string? Url = null);
