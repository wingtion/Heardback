namespace Heardback.Modules.Applications.Contracts;

/// <summary>One status change after the submission, as the timeline shows it.</summary>
/// <param name="Stage">The new status.</param>
/// <param name="OccurredOn">The date the user gave it.</param>
public sealed record ApplicationStatusChange(ApplicationStage Stage, DateOnly OccurredOn);
