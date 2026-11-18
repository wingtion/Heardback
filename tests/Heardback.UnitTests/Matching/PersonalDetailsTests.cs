using Heardback.Modules.Matching.Cvs.Privacy;

namespace Heardback.UnitTests.Matching;

public sealed class PersonalDetailsTests
{
    [Theory]
    [InlineData("Email: ada.lovelace+cv@example.co.uk", "Email: [email]")]
    [InlineData("gül.yılmaz@örnek.com.tr", "[email]")]
    public void Strip_EmailAddresses_AreReplaced(string text, string expected)
    {
        Assert.Equal(expected, PersonalDetails.Strip(text));
    }

    [Theory]
    [InlineData("+90 532 123 45 67")]
    [InlineData("0 (532) 123 45 67")]
    [InlineData("05321234567")]
    [InlineData("+44 20 7946 0958")]
    [InlineData("(555) 123-4567")]
    [InlineData("+1.555.123.4567")]
    public void Strip_PhoneNumbers_AreReplaced(string phone)
    {
        Assert.Equal($"Phone: {PersonalDetails.PhoneMarker}", PersonalDetails.Strip($"Phone: {phone}"));
    }

    [Fact]
    public void Strip_NationalIdNumber_IsReplaced()
    {
        Assert.Equal("TC: [phone]", PersonalDetails.Strip("TC: 12345678901"));
    }

    [Theory]
    [InlineData("2019 - 2023 Backend Developer")]
    [InlineData("2015-2019 2020-2023")]
    [InlineData("Graduated 06.2021, GPA 3.45/4.00")]
    [InlineData("Angular 17.3.1, .NET 8, SQL Server 2019")]
    [InlineData("Led 12 engineers, budget 250000 TRY")]
    public void Strip_YearsDatesVersionsAndShortNumbers_AreKept(string text)
    {
        Assert.Equal(text, PersonalDetails.Strip(text));
    }

    [Theory]
    [InlineData("https://www.linkedin.com/in/ada-lovelace/")]
    [InlineData("http://ada.dev")]
    [InlineData("www.adalovelace.com")]
    [InlineData("linkedin.com/in/ada-lovelace")]
    [InlineData("github.com/ada")]
    [InlineData("behance.net/ada")]
    public void Strip_Links_AreReplaced(string link)
    {
        Assert.Equal($"Profile: {PersonalDetails.LinkMarker}", PersonalDetails.Strip($"Profile: {link}"));
    }

    [Theory]
    [InlineData("ASP.NET Core, ASP.NET/MVC, Node.js/Express, Vue.js")]
    [InlineData("C#/.NET, React/Redux, CI/CD")]
    public void Strip_TechnologyNamesWithDotsAndSlashes_AreKept(string text)
    {
        Assert.Equal(text, PersonalDetails.Strip(text));
    }

    [Theory]
    [InlineData("Caferağa Mah. Moda Cad. No: 12 Daire 3, Kadıköy / İstanbul")]
    [InlineData("Atatürk Bulvarı 45/7 Çankaya Ankara")]
    [InlineData("221B Baker Street, London NW1 6XE")]
    [InlineData("1600 Amphitheatre Pkwy Suite 100, Mountain View")]
    public void Strip_AddressLines_AreReplacedWhole(string address)
    {
        Assert.Equal($"Ada Lovelace\n{PersonalDetails.AddressMarker}\nSkills", PersonalDetails.Strip($"Ada Lovelace\n{address}\nSkills"));
    }

    [Theory]
    [InlineData("Built a product roadmap for 2024")]
    [InlineData("Aptitude test winner 2020")]
    [InlineData("Wall Street Journal")]
    public void Strip_LinesThatOnlyMentionAddressWords_AreKept(string text)
    {
        Assert.Equal(text, PersonalDetails.Strip(text));
    }

    [Fact]
    public void Strip_TypicalCvHeader_KeepsTheNameAndSkills()
    {
        var cv = string.Join(
            '\n',
            "Ada Lovelace",
            "ada@example.com | +90 532 123 45 67 | linkedin.com/in/ada",
            "Moda Cad. No: 12, Kadıköy, İstanbul",
            "Skills: C#, .NET 8, PostgreSQL, Docker");

        Assert.Equal(
            "Ada Lovelace\n[email] | [phone] | [link]\n[address]\nSkills: C#, .NET 8, PostgreSQL, Docker",
            PersonalDetails.Strip(cv));
    }
}
