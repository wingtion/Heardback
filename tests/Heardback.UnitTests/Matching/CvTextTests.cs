using Heardback.Modules.Matching.Cvs.Extraction;

namespace Heardback.UnitTests.Matching;

public sealed class CvTextTests
{
    [Fact]
    public void Normalize_MixedLineEndsTabsAndSpaces_CollapsesThem()
    {
        var text = "  Ada\tLovelace  \r\nBackend   developer\rLondon  UK  ";

        Assert.Equal("Ada Lovelace\nBackend developer\nLondon UK", CvText.Normalize(text));
    }

    [Fact]
    public void Normalize_ManyBlankLines_KeepsOneBlankLine()
    {
        Assert.Equal("Skills\n\nExperience", CvText.Normalize("Skills\n\n\n\n  \nExperience\n\n"));
    }

    [Fact]
    public void Normalize_NulAndInvisibleCharacters_BecomeSpaces()
    {
        Assert.Equal("C# .NET", CvText.Normalize("C#\u0000​.NET"));
    }

    [Fact]
    public void Normalize_DecomposedLetters_AreComposed()
    {
        // "Gül" with the umlaut as a separate combining mark, as some PDFs store it.
        Assert.Equal("Gül", CvText.Normalize("Gül"));
    }
}
