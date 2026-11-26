using Heardback.Modules.Applications.Create;
using Heardback.Modules.Applications.Timeline;

namespace Heardback.Modules.Applications.Samples;

/// <summary>
/// Adds one sample application to the current user's list through the normal create and update handlers, so it
/// follows every rule a real application does.
/// </summary>
internal sealed class SampleApplicationWriter(CreateApplicationHandler create, AddEventHandler addEvent)
{
    /// <returns>False when the user already tracks this posting, so nothing was added.</returns>
    public async Task<bool> AddAsync(SampleApplication sample, DateOnly today, CancellationToken cancellationToken)
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
            return false;
        }

        foreach (var update in sample.Updates)
        {
            await addEvent.HandleAsync(
                created.Value.Id,
                new AddEventRequest(update.Status, today.AddDays(-update.DaysAgo), update.Note),
                cancellationToken);
        }

        return true;
    }
}
