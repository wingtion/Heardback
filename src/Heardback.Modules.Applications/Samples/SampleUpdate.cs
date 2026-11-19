using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Samples;

/// <param name="Status">The new status, or null for a note.</param>
/// <param name="DaysAgo">Days before today the update happened.</param>
/// <param name="Note">The update's note.</param>
internal sealed record SampleUpdate(ApplicationStatus? Status, int DaysAgo, string Note);
