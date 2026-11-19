using Heardback.Modules.Applications.Create;
using Heardback.Modules.Applications.Timeline;
using Microsoft.Extensions.Logging;

namespace Heardback.Modules.Applications.Samples;

/// <summary>
/// Adds the sample applications to the signed-in admin's own list, through the normal create and update handlers
/// so they follow every rule a real application does. Ones the user already tracks are skipped, so pressing the
/// button twice adds nothing twice. No LLM call, no quota.
/// </summary>
internal sealed partial class AddSampleApplicationsHandler(
    CreateApplicationHandler create,
    AddEventHandler addEvent,
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
            var created = await create.HandleAsync(
                new CreateApplicationRequest(
                    sample.Company,
                    sample.Role,
                    sample.Url,
                    sample.PostingText,
                    sample.Channel,
                    today.AddDays(-sample.AppliedDaysAgo)),
                cancellationToken);
            if (!created.IsSuccess)
            {
                skipped++;
                continue;
            }

            foreach (var update in sample.Updates)
            {
                await addEvent.HandleAsync(
                    created.Value.Id,
                    new AddEventRequest(update.Status, today.AddDays(-update.DaysAgo), update.Note),
                    cancellationToken);
            }

            added++;
        }

        LogAdded(added, skipped);
        return new SampleApplicationsResponse(added, skipped);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sample applications added: {Added}, skipped: {Skipped}")]
    private partial void LogAdded(int added, int skipped);
}
