using Heardback.Modules.Matching;
using Heardback.Modules.Matching.Cvs.Extraction;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Heardback.UnitTests.Matching;

public sealed class CvTextExtractorTests
{
    private static CvTextExtractor Extractor(Action<MatchingSettings>? configure = null)
    {
        var settings = new MatchingSettings();
        configure?.Invoke(settings);
        return new CvTextExtractor(Options.Create(settings), NullLogger<CvTextExtractor>.Instance);
    }

    [Fact]
    public void Extract_TwoPagePdf_ReturnsTheTextInReadingOrderAndThePageCount()
    {
        var pdf = TestPdfs.WithPages(
            ["Ada Lovelace", "Backend developer", "Skills: C#, .NET, PostgreSQL"],
            ["Experience", "Analytical Engines Ltd"]);

        var result = Extractor().Extract(pdf);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.PageCount);
        Assert.Equal(
            "Ada Lovelace\nBackend developer\nSkills: C#, .NET, PostgreSQL\n\nExperience\nAnalytical Engines Ltd",
            result.Value.Text);
    }

    [Fact]
    public void Extract_NotAPdf_ReturnsNotPdf()
    {
        var result = Extractor().Extract("PK\u0003\u0004 this is a .docx"u8.ToArray());

        Assert.Equal(MatchingErrors.CvNotPdf, result.Error);
    }

    [Fact]
    public void Extract_LargerThanTheLimit_ReturnsTooLargeBeforeReadingIt()
    {
        var pdf = TestPdfs.WithLines("Ada Lovelace");

        var result = Extractor(settings => settings.CvMaxBytes = pdf.Length - 1).Extract(pdf);

        Assert.Equal("matching.cv_too_large", result.Error!.Code);
    }

    [Fact]
    public void Extract_MorePagesThanTheLimit_ReturnsTooManyPages()
    {
        var pdf = TestPdfs.WithPages(["one"], ["two"], ["three"]);

        var result = Extractor(settings => settings.CvMaxPages = 2).Extract(pdf);

        Assert.Equal("matching.cv_too_many_pages", result.Error!.Code);
        Assert.Equal("The PDF has more than 2 pages.", result.Error!.Message);
    }

    [Fact]
    public void Extract_PagesWithoutText_ReturnsNoText()
    {
        var result = Extractor().Extract(TestPdfs.WithPages([], []));

        Assert.Equal(MatchingErrors.CvNoText, result.Error);
    }

    [Fact]
    public void Extract_MoreTextThanTheLimit_ReturnsTooMuchText()
    {
        var lines = Enumerable.Range(0, 40).Select(line => $"Line {line} of a very long CV page").ToArray();

        var result = Extractor(settings => settings.CvMaxTextLength = 1000).Extract(TestPdfs.WithLines(lines));

        Assert.Equal("matching.cv_too_much_text", result.Error!.Code);
    }

    [Fact]
    public void Extract_DamagedPdf_ReturnsUnreadable()
    {
        var damaged = "%PDF-1.7\n1 0 obj << /Type /Catalog"u8.ToArray();

        var result = Extractor().Extract(damaged);

        Assert.Equal(MatchingErrors.CvUnreadable, result.Error);
    }

    [Fact]
    public void Extract_StreamWithinTheDecodeBudget_IsRead()
    {
        var pdf = TestPdfs.WithFlateContentOf(512 * 1024);

        var result = Extractor(settings => settings.CvMaxDecodedBytes = 1024 * 1024).Extract(pdf);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello", result.Value.Text);
    }

    [Fact]
    public void Extract_SmallPdfThatUnpacksPastTheBudget_ReturnsTooComplex()
    {
        // About 60 KB on disk, 64 MB once unpacked: a decompression bomb.
        var pdf = TestPdfs.WithFlateContentOf(64 * 1024 * 1024);

        var result = Extractor(settings => settings.CvMaxDecodedBytes = 1024 * 1024).Extract(pdf);

        Assert.True(pdf.Length < 200 * 1024);
        Assert.Equal(MatchingErrors.CvTooComplex, result.Error);
    }

    [Fact]
    public void Extract_LzwCompressedStream_ReturnsTooComplex()
    {
        var result = Extractor().Extract(TestPdfs.WithLzwContent());

        Assert.Equal(MatchingErrors.CvTooComplex, result.Error);
    }
}
