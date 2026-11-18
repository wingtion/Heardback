using Heardback.Modules.Postings.Contracts;
using Heardback.Shared.Results;

namespace Heardback.Modules.Postings;

internal static class PostingErrors
{
    public const string UrlField = "url";

    public static readonly Error InvalidUrl =
        Error.Validation("postings.invalid_url", UrlField, PostingUrlAttribute.Message);
}
