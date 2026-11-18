using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Matching;

/// <summary>Limits for CV uploads (decided 2026-11-18; see ADR-0006).</summary>
internal sealed class MatchingSettings
{
    public const string SectionName = "Matching";

    /// <summary>Largest CV PDF accepted, in bytes (5 MB).</summary>
    [Range(1, 50 * 1024 * 1024)]
    public int CvMaxBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Most pages a CV PDF may have.</summary>
    [Range(1, 100)]
    public int CvMaxPages { get; set; } = 10;

    /// <summary>Most characters of text kept from a CV; a longer PDF is rejected rather than cut.</summary>
    [Range(1000, 80_000)]
    public int CvMaxTextLength { get; set; } = 50_000;

    /// <summary>
    /// How many bytes all compressed streams of one PDF may unpack to while reading it. Stops a small PDF
    /// from unpacking into gigabytes ("decompression bomb").
    /// </summary>
    [Range(1024 * 1024, 512 * 1024 * 1024)]
    public int CvMaxDecodedBytes { get; set; } = 32 * 1024 * 1024;

    /// <summary>CV uploads one user may make per hour.</summary>
    [Range(1, 1000)]
    public int CvUploadsPerHour { get; set; } = 5;

    /// <summary>Other CV changes (skills marked as not yours, deletes) one user may make per minute.</summary>
    [Range(1, 10_000)]
    public int CvWritesPerMinute { get; set; } = 60;
}
