namespace JobTrail.Shared.Persistence;

/// <summary>One module's database schema, as seen by the host: lets it migrate every module without knowing their DbContexts.</summary>
public interface IModuleDatabase
{
    string Name { get; }

    Task MigrateAsync(CancellationToken cancellationToken);

    /// <summary>True when the model has changes that no migration captures yet.</summary>
    bool HasPendingModelChanges();
}
