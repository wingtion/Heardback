using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Applications.Samples;

/// <summary>
/// Adds the sample applications to the signed-in admin's own list. Ones the user already tracks are skipped, so
/// pressing the button twice adds nothing twice. No LLM call, no quota.
/// </summary>
internal sealed partial class AddSampleApplicationsHandler(
    SampleApplicationWriter writer,
    TimeProvider timeProvider,
    ILogger<AddSampleApplicationsHandler> logger)
{
    public async Task<SampleApplicationsResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var added = 0;
        var skipped = 0;

        foreach (var sample in SampleApplications.All)
        {
            if (await writer.AddAsync(sample, today, cancellationToken))
            {
                added++;
            }
            else
            {
                skipped++;
            }
        }

        LogAdded(added, skipped);
        return new SampleApplicationsResponse(added, skipped);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sample applications added: {Added}, skipped: {Skipped}")]
    private partial void LogAdded(int added, int skipped);
}
