namespace Heardback.Modules.Applications.Read;

/// <summary>
/// The list's Status filter. <see cref="NoResponse"/> and <see cref="Applied"/> split the Applied status the
/// same way the table labels do.
/// </summary>
internal enum ApplicationListStatus
{
    Applied,
    NoResponse,
    Interview,
    Offer,
    Rejected,
}
