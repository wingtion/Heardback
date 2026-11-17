namespace Heardback.UnitTests.Architecture;

internal static class ModuleCatalog
{
    private const string SolutionFileName = "Heardback.slnx";
    private const string SourceFolderName = "src";

    public static IReadOnlyList<ModuleAssembly> LoadFromTestOutput() =>
        Directory.EnumerateFiles(AppContext.BaseDirectory, $"{ModuleAssembly.NamePrefix}*.dll")
            .Select(path => new ModuleAssembly(Path.GetFileNameWithoutExtension(path), path))
            .OrderBy(module => module.Name, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<string> ModuleProjectNamesInSource() =>
        Directory.EnumerateDirectories(
                Path.Combine(FindRepositoryRoot(), SourceFolderName),
                $"{ModuleAssembly.NamePrefix}*")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"Could not find {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
