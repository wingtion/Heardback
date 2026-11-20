using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Insights.Community;

/// <summary>The ghost-listing thresholds (ADR-0013). The minimum group size lives with the data, in Applications.</summary>
internal sealed class CommunitySettings
{
    public const string SectionName = "Community";

    /// <summary>A posting first seen at least this many days ago earns the "listed a long time" point.</summary>
    [Range(1, 365)]
    public int LongListedDays { get; set; } = 30;

    /// <summary>A response rate below this percent counts as low, for the posting and for the company.</summary>
    [Range(1, 100)]
    public int LowResponsePercent { get; set; } = 25;

    /// <summary>A company answering in this many days or more on average counts as slow.</summary>
    [Range(1, 365)]
    public int SlowResponseDays { get; set; } = 21;

    /// <summary>A role linked to at least this many separate postings at one company counts as posted again.</summary>
    [Range(2, 100)]
    public int RepostedRolePostings { get; set; } = 3;
}
