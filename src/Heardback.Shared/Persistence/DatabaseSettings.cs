using System.ComponentModel.DataAnnotations;

namespace Heardback.Shared.Persistence;

public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    /// <summary>Npgsql connection string. Comes from user-secrets locally and an environment variable in production.</summary>
    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Apply pending EF Core migrations for every module when the API starts.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
