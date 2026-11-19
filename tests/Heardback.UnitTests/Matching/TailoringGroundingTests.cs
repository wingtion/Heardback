using Heardback.Modules.Matching.Tailoring;

namespace Heardback.UnitTests.Matching;

public sealed class TailoringGroundingTests
{
    private const string Cv = """
        Experience
        Contoso Logistics — Backend Developer (2022–present)
        - Cut PostgreSQL query times by 60%   by rewriting reports.
        Skills
        Java, Python, C#, SQL, Git
        """;

    private static readonly TailoringInput Input =
        new(Cv, "Northwind", "Backend Engineer", "Posting", ["C#"], ["Kubernetes", "Go", "GitHub Actions"]);

    [Theory]
    [InlineData("Cut PostgreSQL query times by 60% by rewriting reports")]
    [InlineData("cut postgresql   QUERY times")]
    [InlineData("“Contoso Logistics - Backend Developer”")]
    [InlineData("Java, Python, C#…")]
    [InlineData("Contoso Logistics ‑ Backend Developer")] // non-breaking hyphen, as a model wrote it
    [InlineData("Contoso Logistics ‐ Backend Developer (2022–present)")]
    public void Keep_QuoteFromTheCv_IsKept(string quote)
    {
        var kept = TailoringGrounding.Keep([new TailoringAdvice("Experience", quote, "Move this first.")], Input);

        Assert.Single(kept);
    }

    [Theory]
    [InlineData("Designed event-driven services on Azure")]
    [InlineData("C")]
    [InlineData("  ")]
    public void Keep_QuoteNotInTheCv_IsDropped(string quote)
    {
        Assert.Empty(TailoringGrounding.Keep([new TailoringAdvice("Experience", quote, "Move this first.")], Input));
    }

    [Theory]
    [InlineData("Add Kubernetes to the skills list.")]
    [InlineData("Mention kubernetes if you used it.")]
    [InlineData("Say you set up GitHub Actions pipelines.")]
    [InlineData("List Go first.")]
    public void Keep_AdviceNamingAMissingSkill_IsDropped(string advice)
    {
        Assert.Empty(TailoringGrounding.Keep([new TailoringAdvice("Skills", "Java, Python, C#", advice)], Input));
    }

    [Theory]
    [InlineData("Put C# first; it is a good fit.")]
    [InlineData("Group the languages under one line.")]
    public void Keep_AdviceOnlyReworkingTheCv_IsKept(string advice)
    {
        Assert.Single(TailoringGrounding.Keep([new TailoringAdvice("Skills", "Java, Python, C#", advice)], Input));
    }

    [Theory]
    [InlineData("Use C# first", "C#", true)]
    [InlineData("A good fit", "Go", false)]
    [InlineData("Go is missing", "Go", true)]
    [InlineData("Built on .NET 10", ".NET", true)]
    [InlineData("ASP.NET Core", ".NET", true)]
    [InlineData("Javascript", "Java", false)]
    public void Names_WholeWordsOnly(string text, string skill, bool expected)
    {
        Assert.Equal(expected, TailoringGrounding.Names(text, skill));
    }
}
