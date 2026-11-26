using Heardback.Modules.Applications;
using Heardback.Modules.Matching;
using Heardback.UnitTests.Shared;

namespace Heardback.UnitTests.Applications;

/// <summary>Demo visitors get the small AI quotas, everyone else the normal ones (ADR-0018).</summary>
public sealed class DemoQuotaTests
{
    [Theory]
    [InlineData(false, 30)]
    [InlineData(true, 3)]
    public void PostingAnalysesPerDayFor_ByAccountKind_UsesTheMatchingLimit(bool isDemo, int expected)
    {
        var settings = new ApplicationsSettings { PostingAnalysesPerDay = 30, DemoPostingAnalysesPerDay = 3 };

        var limit = settings.PostingAnalysesPerDayFor(new FakeCurrentUser(isDemo));

        Assert.Equal(expected, limit);
    }

    [Theory]
    [InlineData(false, 10)]
    [InlineData(true, 2)]
    public void TailoringPerDayFor_ByAccountKind_UsesTheMatchingLimit(bool isDemo, int expected)
    {
        var settings = new MatchingSettings { TailoringPerDay = 10, DemoTailoringPerDay = 2 };

        var limit = settings.TailoringPerDayFor(new FakeCurrentUser(isDemo));

        Assert.Equal(expected, limit);
    }
}
