namespace Heardback.Modules.Applications.Samples;

/// <param name="Added">Sample applications added now.</param>
/// <param name="Skipped">Samples the user already tracked.</param>
internal sealed record SampleApplicationsResponse(int Added, int Skipped);
