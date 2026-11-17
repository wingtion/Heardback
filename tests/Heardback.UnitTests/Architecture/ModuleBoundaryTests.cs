namespace Heardback.UnitTests.Architecture;

public sealed class ModuleBoundaryTests
{
    private readonly IReadOnlyList<ModuleAssembly> _modules = ModuleCatalog.LoadFromTestOutput();

    [Fact]
    public void LoadFromTestOutput_AllModuleProjects_AreInspected()
    {
        var inspected = _modules.Select(module => module.Name).ToList();

        Assert.Equal(ModuleCatalog.ModuleProjectNamesInSource(), inspected);
    }

    [Fact]
    public void ExportedTypes_EveryModule_AreOnlyContractsOrRegistrationEntryPoint()
    {
        var violations = _modules
            .SelectMany(module => module.Load().GetExportedTypes()
                .Where(type => !module.IsContract(type.Namespace)
                    && type.FullName != module.RegistrationTypeFullName)
                .Select(type => $"{module}: {type.FullName} is public but not in {module.ContractsNamespace}"))
            .ToList();

        AssertNoViolations(violations);
    }

    [Fact]
    public void TypeReferences_BetweenModules_TargetOnlyContracts()
    {
        var modulesByName = _modules.ToDictionary(module => module.Name, StringComparer.Ordinal);

        var violations = _modules
            .SelectMany(module => ExternalTypeReference.ReadAll(module.FilePath)
                .Where(reference => modulesByName.TryGetValue(reference.AssemblyName, out var target)
                    && target != module
                    && !target.IsContract(reference.Namespace))
                .Select(reference => $"{module} uses non-contract type {reference}"))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        AssertNoViolations(violations);
    }

    // Assert.Empty truncates long items; list every violation in full so the fix is obvious.
    private static void AssertNoViolations(List<string> violations)
    {
        if (violations.Count > 0)
        {
            Assert.Fail($"Module boundary violations:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
        }
    }
}
