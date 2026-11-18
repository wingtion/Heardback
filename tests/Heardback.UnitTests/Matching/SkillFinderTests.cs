using Heardback.Modules.Matching.Skills;

namespace Heardback.UnitTests.Matching;

public sealed class SkillFinderTests
{
    private static string[] Names(string text) => SkillFinder.Find(text).Select(skill => skill.Name).ToArray();

    [Theory]
    [InlineData("C#, C++ and C", new[] { "C#", "C++", "C" })]
    [InlineData("Languages: C/C++", new[] { "C++", "C" })]
    [InlineData("Java and JavaScript", new[] { "Java", "JavaScript" })]
    [InlineData("JavaScript developer", new[] { "JavaScript" })]
    [InlineData("React and React Native apps", new[] { "React", "React Native" })]
    [InlineData("React Native", new[] { "React Native" })]
    [InlineData("Angular 17 and AngularJS", new[] { "Angular", "AngularJS" })]
    [InlineData("Go, Rust and Golang", new[] { "Go", "Rust" })]
    [InlineData("C#/.NET 8, ASP.NET Core Web API", new[] { "C#", ".NET", "ASP.NET Core", "REST APIs" })]
    [InlineData("SQL Server, T-SQL, PostgreSQL", new[] { "T-SQL", "PostgreSQL", "SQL Server" })]
    [InlineData("Strong SQL", new[] { "SQL" })]
    [InlineData("RESTful APIs with Node.js and Express.js", new[] { "Node.js", "Express", "REST APIs" })]
    [InlineData("Spring Boot microservices on Kubernetes", new[] { "Spring Boot", "Kubernetes", "Microservices" })]
    [InlineData("C# ve .NET ile geliştirme, Docker ve Azure", new[] { "C#", ".NET", "Docker", "Azure" })]
    [InlineData("react  native\nand vue.js", new[] { "React Native", "Vue.js" })]
    public void Find_TechnicalText_FindsEachSkillOnce(string text, string[] expected)
    {
        Assert.Equal(expected, Names(text));
    }

    [Theory]
    [InlineData("Worked with C-level executives on a go-to-market plan")]
    [InlineData("Go-getter, led R&D for the rest of the team")]
    [InlineData("Express interest early; I excel at planning in the spring")]
    [InlineData("Implemented a tree node walker in the c drive")]
    [InlineData("A team with unity of purpose and swift delivery")]
    public void Find_OrdinaryWords_AreNotSkills(string text)
    {
        Assert.Empty(Names(text));
    }

    [Fact]
    public void WithImplied_FrameworkSkills_AddWhatTheyRequireTransitively()
    {
        var implied = SkillFinder.WithImplied(SkillFinder.Find("ASP.NET Core, Next.js, GitHub Actions"));

        Assert.Equal(
            ["JavaScript", ".NET", "ASP.NET Core", "React", "Next.js", "CI/CD", "GitHub Actions", "Git"],
            implied.Select(skill => skill.Name));
    }
}
