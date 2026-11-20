using Heardback.Modules.Applications.Domain;

namespace Heardback.UnitTests.Applications;

public sealed class NameKeysTests
{
    [Theory]
    [InlineData("Acme")]
    [InlineData("ACME")]
    [InlineData("  acme  ")]
    [InlineData("Acme Inc.")]
    [InlineData("Acme, Inc")]
    [InlineData("Acme LLC")]
    [InlineData("Acme Ltd.")]
    [InlineData("Acme GmbH")]
    [InlineData("Acme & Co.")]
    [InlineData("Acme Corp")]
    public void Company_SpellingAndLegalFormVariants_GiveSameKey(string name)
    {
        var key = NameKeys.Company(name);

        Assert.Equal("acme", key);
    }

    [Theory]
    [InlineData("Şişecam A.Ş.")]
    [InlineData("Sisecam AS")]
    [InlineData("ŞİŞECAM")]
    [InlineData("Şişecam Sanayi ve Ticaret A.Ş.")]
    [InlineData("Şişecam San. ve Tic. Ltd. Şti.")]
    public void Company_TurkishLettersAndLegalForms_GiveSameKey(string name)
    {
        var key = NameKeys.Company(name);

        Assert.Equal("sisecam", key);
    }

    [Fact]
    public void Company_DotlessI_FoldsToPlainI()
    {
        var withDotless = NameKeys.Company("Işık Yazılım");
        var withoutTurkishLetters = NameKeys.Company("Isik Yazilim");

        Assert.Equal(withoutTurkishLetters, withDotless);
        Assert.Equal("isik yazilim", withDotless);
    }

    [Fact]
    public void Company_DecomposedUnicode_GivesSameKeyAsComposed()
    {
        // "Ş" as one code point versus "S" followed by a combining cedilla.
        var composed = NameKeys.Company("Şirket");
        var decomposed = NameKeys.Company("Şirket");

        Assert.Equal(composed, decomposed);
    }

    [Fact]
    public void Company_SuffixWordInsideName_IsKept()
    {
        var key = NameKeys.Company("Co Working Labs");

        Assert.Equal("co working labs", key);
    }

    [Fact]
    public void Company_OnlyLegalFormWords_KeepsThemInsteadOfBecomingEmpty()
    {
        var key = NameKeys.Company("AG");

        Assert.Equal("ag", key);
    }

    [Fact]
    public void Company_DifferentCompanies_GiveDifferentKeys()
    {
        var first = NameKeys.Company("Northwind Logistics");
        var second = NameKeys.Company("Northwind Traders");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Role_PunctuationAndCase_AreFoldedButLegalWordsKept()
    {
        var key = NameKeys.Role("Senior Backend Engineer (.NET) – Co-op");

        Assert.Equal("senior backend engineer net co op", key);
    }
}
