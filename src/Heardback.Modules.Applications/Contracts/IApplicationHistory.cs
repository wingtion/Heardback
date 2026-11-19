namespace Heardback.Modules.Applications.Contracts;

/// <summary>
/// Read access to the signed-in user's applications and their status changes, for personal insights (ADR-0011).
/// Other users' applications are never visible through it.
/// </summary>
public interface IApplicationHistory
{
    /// <summary>All of the signed-in user's applications with their status changes.</summary>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>The applications, newest applied first.</returns>
    Task<IReadOnlyList<ApplicationHistory>> ListAsync(CancellationToken cancellationToken);
}
