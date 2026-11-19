namespace Heardback.Modules.Applications.Contracts;

/// <summary>An application status as other modules see it.</summary>
public enum ApplicationStage
{
    /// <summary>Sent, no answer recorded.</summary>
    Applied,

    /// <summary>In an interview process.</summary>
    Interview,

    /// <summary>An offer was made.</summary>
    Offer,

    /// <summary>Turned down.</summary>
    Rejected,
}
