using Heardback.Modules.Matching.Skills;

namespace Heardback.UnitTests.Matching;

public sealed class SkillCatalogTests
{
    [Fact]
    public void All_Names_AreUnique()
    {
        var duplicates = SkillCatalog.All.GroupBy(skill => skill.Name).Where(group => group.Count() > 1);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void All_EverySkill_HasAtLeastOneAlias()
    {
        Assert.All(SkillCatalog.All, skill => Assert.NotEmpty(skill.Aliases.Concat(skill.ExactAliases)));
    }

    [Fact]
    public void All_Aliases_BelongToOneSkillOnly()
    {
        var shared = SkillCatalog.All
            .SelectMany(skill => skill.Aliases.Concat(skill.ExactAliases).Select(alias => (alias, skill.Name)))
            .GroupBy(entry => entry.alias, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(entry => entry.Name).Distinct().Count() > 1)
            .Select(group => group.Key);

        Assert.Empty(shared);
    }

    [Fact]
    public void All_ImpliedSkills_Exist()
    {
        var names = SkillCatalog.All.Select(skill => skill.Name).ToHashSet();

        Assert.All(SkillCatalog.All, skill => Assert.Subset(names, skill.Implies.ToHashSet()));
    }

    [Fact]
    public void All_EverySkill_FindsItsOwnAliases()
    {
        Assert.All(
            SkillCatalog.All,
            skill => Assert.All(
                skill.Aliases.Concat(skill.ExactAliases),
                alias => Assert.Contains(skill, SkillFinder.Find($"Skills: {alias}, and more"))));
    }
}
