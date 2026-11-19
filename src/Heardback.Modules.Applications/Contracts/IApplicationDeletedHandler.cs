namespace Heardback.Modules.Applications.Contracts;

/// <summary>
/// Lets another module remove what it keeps about an application when the user deletes it (for example tailoring
/// suggestions made from the CV). Called in the deleting request, for the signed-in user's own application.
/// </summary>
public interface IApplicationDeletedHandler
{
    /// <summary>Removes the module's data for <paramref name="applicationId"/>.</summary>
    /// <param name="applicationId">The deleted application's id.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>A task that completes when the data is gone.</returns>
    Task OnDeletedAsync(Guid applicationId, CancellationToken cancellationToken);
}
