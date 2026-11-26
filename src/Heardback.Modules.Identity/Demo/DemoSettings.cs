using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Identity.Demo;

/// <summary>"Try the demo" sandboxes (ADR-0018), under <c>Identity:Demo</c>. Off unless a deployment turns it on.</summary>
internal sealed class DemoSettings
{
    public const string SectionName = "Identity:Demo";

    public bool Enabled { get; set; }

    /// <summary>How long a demo visitor's account and data live before the cleanup removes them.</summary>
    [Range(typeof(TimeSpan), "00:10:00", "7.00:00:00")]
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Demo visitors alive at once. Keeps a burst of visitors from filling the free database tier.</summary>
    [Range(1, 1000)]
    public int MaxActiveVisitors { get; set; } = 20;

    /// <summary>New demos per client IP per <see cref="IpWindow"/>.</summary>
    [Range(1, 100)]
    public int VisitorsPerIp { get; set; } = 3;

    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan IpWindow { get; set; } = TimeSpan.FromHours(1);

    /// <summary>How often expired demo visitors are removed while the API is running (also once at startup).</summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
