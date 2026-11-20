using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Applications;

internal sealed class ApplicationsSettings
{
    public const string SectionName = "Applications";

    /// <summary>Days without any event before an Applied application shows "No response · N days".</summary>
    [Range(1, 365)]
    public int NoResponseAfterDays { get; set; } = 14;

    /// <summary>
    /// Fewest other members a community group needs before any of its numbers are shown (ADR-0013). Never below 3,
    /// so no one or two people can be shown on their own.
    /// </summary>
    [Range(3, 1000)]
    public int CommunityMinimumMembers { get; set; } = 3;

    /// <summary>Most cards a board column returns; the column count is always the real total (docs/design.md §14).</summary>
    [Range(1, 1000)]
    public int BoardColumnLimit { get; set; } = 200;

    /// <summary>Adds, edits, deletes and timeline updates one user may make per minute.</summary>
    [Range(1, 10_000)]
    public int WritesPerMinute { get; set; } = 60;

    /// <summary>
    /// Posting analyses one user may ask for per UTC day (ADR-0008). Text someone already had analysed is free;
    /// a failed analysis gives its use back.
    /// </summary>
    [Range(1, 1000)]
    public int PostingAnalysesPerDay { get; set; } = 30;

    /// <summary>
    /// Lets admins add sample applications to their own list for trying the app out. Off unless configured: on in
    /// appsettings.Development.json, off in production.
    /// </summary>
    public bool SampleDataEnabled { get; set; }
}
