namespace Heardback.Modules.Applications.Samples;

/// <param name="Neighbor">Which demo neighbor applied, from 1.</param>
/// <param name="Company">The company of the sample posting applied to.</param>
/// <param name="AppliedDaysAgo">Days before the neighbor was created that the application was sent.</param>
/// <param name="Updates">Timeline updates after the submission, oldest first.</param>
internal sealed record NeighborApplication(
    int Neighbor,
    string Company,
    int AppliedDaysAgo,
    IReadOnlyList<SampleUpdate> Updates);
