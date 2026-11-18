using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Applications;

internal sealed class ApplicationsSettings
{
    public const string SectionName = "Applications";

    /// <summary>Days without any event before an Applied application shows "No response · N days".</summary>
    [Range(1, 365)]
    public int NoResponseAfterDays { get; set; } = 14;

    /// <summary>Most cards a board column returns; the column count is always the real total (docs/design.md §14).</summary>
    [Range(1, 1000)]
    public int BoardColumnLimit { get; set; } = 200;

    /// <summary>Adds, edits, deletes and timeline updates one user may make per minute.</summary>
    [Range(1, 10_000)]
    public int WritesPerMinute { get; set; } = 60;
}
