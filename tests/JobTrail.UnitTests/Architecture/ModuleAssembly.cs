using System.Reflection;

namespace JobTrail.UnitTests.Architecture;

internal sealed record ModuleAssembly(string Name, string FilePath)
{
    public const string NamePrefix = "JobTrail.Modules.";

    public string ContractsNamespace => $"{Name}.Contracts";

    /// <summary>The only public non-contract type a module may expose: its DI registration class.</summary>
    public string RegistrationTypeFullName => $"{Name}.{Name[NamePrefix.Length..]}Module";

    public Assembly Load() => Assembly.Load(new AssemblyName(Name));

    public bool IsContract(string? typeNamespace) =>
        typeNamespace is not null
        && (typeNamespace == ContractsNamespace
            || typeNamespace.StartsWith($"{ContractsNamespace}.", StringComparison.Ordinal));

    public override string ToString() => Name;
}
