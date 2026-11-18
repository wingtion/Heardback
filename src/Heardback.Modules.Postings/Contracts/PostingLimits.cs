namespace Heardback.Modules.Postings.Contracts;

/// <summary>Maximum lengths of posting fields, so other modules can validate input with the same limits.</summary>
public static class PostingLimits
{
    /// <summary>Longest accepted posting link.</summary>
    public const int UrlMaxLength = 2048;

    /// <summary>Longest accepted company name.</summary>
    public const int CompanyMaxLength = 200;

    /// <summary>Longest accepted role (job title).</summary>
    public const int TitleMaxLength = 200;

    /// <summary>Longest accepted posting text; generous for a pasted job description.</summary>
    public const int TextMaxLength = 20_000;
}
