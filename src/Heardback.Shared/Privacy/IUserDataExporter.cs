namespace Heardback.Shared.Privacy;

/// <summary>
/// Contributes one module's part of a user's data export (ADR-0015). The Identity module collects every part into
/// one JSON file, each under its <see cref="Section"/> name.
/// </summary>
public interface IUserDataExporter
{
    /// <summary>The part's name in the export file, such as <c>applications</c>; unique across modules.</summary>
    string Section { get; }

    /// <summary>
    /// Reads everything the module keeps about <paramref name="userId"/> that the user can see. Runs in the user's
    /// own request, so the ownership filters apply. Never includes other users' data.
    /// </summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="cancellationToken">Cancels the database work.</param>
    /// <returns>A JSON-serializable object for the section.</returns>
    Task<object> ExportAsync(Guid userId, CancellationToken cancellationToken);
}
