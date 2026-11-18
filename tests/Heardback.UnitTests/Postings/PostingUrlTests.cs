using Heardback.Modules.Postings.Contracts;
using Heardback.Modules.Postings.Domain;

namespace Heardback.UnitTests.Postings;

public sealed class PostingUrlTests
{
    [Theory]
    [InlineData("https://jobs.example.com/4471", "jobs.example.com/4471")]
    [InlineData("  https://jobs.example.com/4471  ", "jobs.example.com/4471")]
    [InlineData("https://WWW.Jobs.Example.com/4471", "jobs.example.com/4471")]
    [InlineData("http://jobs.example.com/4471", "jobs.example.com/4471")]
    [InlineData("https://jobs.example.com/4471/", "jobs.example.com/4471")]
    [InlineData("https://jobs.example.com/4471#apply", "jobs.example.com/4471")]
    [InlineData("https://jobs.example.com:443/4471", "jobs.example.com/4471")]
    [InlineData("https://jobs.example.com/", "jobs.example.com")]
    public void TryNormalize_CosmeticDifferences_GiveSameKey(string url, string expected)
    {
        var normalized = PostingUrl.TryNormalize(url, out var key);

        Assert.True(normalized);
        Assert.Equal(expected, key);
    }

    [Fact]
    public void TryNormalize_TrackingParameters_AreRemoved()
    {
        PostingUrl.TryNormalize(
            "https://jobs.example.com/view?id=7&utm_source=x&UTM_Medium=y&gclid=1&fbclid=2&trk=3&ref=4", out var key);

        Assert.Equal("jobs.example.com/view?id=7", key);
    }

    [Fact]
    public void TryNormalize_ParametersInAnotherOrder_GiveSameKey()
    {
        PostingUrl.TryNormalize("https://jobs.example.com/view?b=2&a=1", out var first);
        PostingUrl.TryNormalize("https://jobs.example.com/view?a=1&b=2", out var second);

        Assert.Equal(first, second);
        Assert.Equal("jobs.example.com/view?a=1&b=2", first);
    }

    [Fact]
    public void TryNormalize_DifferentPathCase_GiveDifferentKeys()
    {
        PostingUrl.TryNormalize("https://jobs.example.com/Job/AB12", out var first);
        PostingUrl.TryNormalize("https://jobs.example.com/job/ab12", out var second);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void TryNormalize_NonDefaultPort_IsKept()
    {
        PostingUrl.TryNormalize("https://jobs.example.com:8443/4471", out var key);

        Assert.Equal("jobs.example.com:8443/4471", key);
    }

    [Theory]
    [InlineData("https://www.linkedin.com/jobs/view/4123456789/")]
    [InlineData("https://tr.linkedin.com/jobs/view/backend-engineer-at-northwind-4123456789?trk=public_jobs")]
    [InlineData("https://www.linkedin.com/jobs/collections/recommended/?currentJobId=4123456789&origin=JYMBII")]
    [InlineData("https://linkedin.com/jobs/search/?keywords=dotnet&currentJobId=4123456789")]
    public void TryNormalize_LinkedInJobLinks_UseTheJobId(string url)
    {
        var normalized = PostingUrl.TryNormalize(url, out var key);

        Assert.True(normalized);
        Assert.Equal("linkedin.com/jobs/view/4123456789", key);
    }

    [Fact]
    public void TryNormalize_LinkedInLinkWithoutJobId_KeepsThePath()
    {
        PostingUrl.TryNormalize("https://www.linkedin.com/company/northwind/jobs/", out var key);

        Assert.Equal("linkedin.com/company/northwind/jobs", key);
    }

    [Theory]
    [InlineData("northwind-logistics/jobs")]
    [InlineData("ftp://jobs.example.com/4471")]
    [InlineData("mailto:jobs@example.com")]
    [InlineData("https://localhost/jobs")]
    [InlineData("not a link")]
    [InlineData("")]
    public void TryNormalize_NotAnHttpLink_ReturnsFalse(string url)
    {
        var normalized = PostingUrl.TryNormalize(url, out var key);

        Assert.False(normalized);
        Assert.Null(key);
    }

    [Fact]
    public void TryNormalize_LongerThanTheLimit_ReturnsFalse()
    {
        var url = "https://jobs.example.com/" + new string('a', PostingLimits.UrlMaxLength);

        Assert.False(PostingUrl.TryNormalize(url, out _));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("https://jobs.example.com/4471", true)]
    [InlineData("northwind-logistics/jobs", false)]
    public void PostingUrlAttribute_IsValid_AcceptsEmptyOrHttpLinks(string? value, bool expected)
    {
        var attribute = new PostingUrlAttribute();

        Assert.Equal(expected, attribute.IsValid(value));
    }
}
