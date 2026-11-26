namespace Heardback.Modules.Identity.Domain;

/// <summary>What an account is for (ADR-0018). Only members are real people who joined with an invite.</summary>
internal enum UserKind
{
    /// <summary>A real person who registered with an invite code.</summary>
    Member = 0,

    /// <summary>
    /// A temporary sandbox from "Try the demo": no password, removed about a day after it was created, and never
    /// part of the community statistics members see.
    /// </summary>
    DemoVisitor = 1,

    /// <summary>
    /// A permanent made-up account whose applications give demo visitors community signals to look at, so a
    /// visitor never sees real members' data. Members never see these either.
    /// </summary>
    DemoNeighbor = 2,
}
