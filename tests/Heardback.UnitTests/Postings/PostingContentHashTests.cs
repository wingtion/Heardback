using Heardback.Modules.Postings.Domain;

namespace Heardback.UnitTests.Postings;

public sealed class PostingContentHashTests
{
    private const string Company = "Northwind Logistics";
    private const string Title = "Backend Engineer (.NET)";
    private const string Text = "We are hiring a backend engineer to work on shipment tracking APIs.";

    [Fact]
    public void ComputeOrNull_SameContent_GivesSameLowerCaseHexHash()
    {
        var first = PostingContentHash.ComputeOrNull(Company, Title, Text);
        var second = PostingContentHash.ComputeOrNull(Company, Title, Text);

        Assert.Equal(first, second);
        Assert.Equal(PostingContentHash.Length, first!.Length);
        Assert.Matches("^[0-9a-f]+$", first);
    }

    [Fact]
    public void ComputeOrNull_DifferentCaseAndWhitespace_GivesSameHash()
    {
        var first = PostingContentHash.ComputeOrNull(Company, Title, Text);
        var second = PostingContentHash.ComputeOrNull(
            "  NORTHWIND   logistics ", "backend engineer (.net)", "We are hiring a backend engineer\n\n to work on\tshipment tracking APIs.  ");

        Assert.Equal(first, second);
    }

    [Fact]
    public void ComputeOrNull_DifferentUnicodeForms_GivesSameHash()
    {
        // "ş" as one code point versus "s" followed by a combining cedilla.
        var composed = PostingContentHash.ComputeOrNull("Şirket", Title, "Yazılım geliştirici arıyoruz");
        var decomposed = PostingContentHash.ComputeOrNull("Şirket", Title, "Yazılım geliştirici arıyoruz");

        Assert.Equal(composed, decomposed);
    }

    [Fact]
    public void ComputeOrNull_DifferentText_GivesDifferentHash()
    {
        var first = PostingContentHash.ComputeOrNull(Company, Title, Text);
        var second = PostingContentHash.ComputeOrNull(Company, Title, Text + " Remote friendly.");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ComputeOrNull_TextMovedBetweenFields_GivesDifferentHash()
    {
        var first = PostingContentHash.ComputeOrNull("Northwind", "Logistics Engineer", Text);
        var second = PostingContentHash.ComputeOrNull("Northwind Logistics", "Engineer", Text);

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ComputeOrNull_NoText_ReturnsNull(string? text)
    {
        Assert.Null(PostingContentHash.ComputeOrNull(Company, Title, text));
    }
}
