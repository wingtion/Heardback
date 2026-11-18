using System.Text.Json.Serialization;

namespace Heardback.Modules.Applications.Domain;

/// <summary>
/// Where an application stands. "No response" is not a status: it is derived from dates for applications that
/// are still <see cref="Applied"/> (see <see cref="NoResponseRule"/>).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApplicationStatus>))]
internal enum ApplicationStatus
{
    Applied,
    Interview,
    Offer,
    Rejected,
}
