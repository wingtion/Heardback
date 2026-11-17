using System.Text.RegularExpressions;

namespace Heardback.IntegrationTests.Postgres;

/// <summary>Reads settings from the repository's compose.yaml, which is the single source of truth for them.</summary>
internal static partial class ComposeFile
{
    private const string FileName = "compose.yaml";

    public static string PostgresImage()
    {
        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        var match = PostgresImagePattern().Match(File.ReadAllText(path));

        return match.Success
            ? match.Groups["image"].Value
            : throw new InvalidOperationException($"No postgres image found in {path}.");
    }

    [GeneratedRegex(@"^\s*image:\s*(?<image>postgres:\S+)\s*$", RegexOptions.Multiline)]
    private static partial Regex PostgresImagePattern();
}
