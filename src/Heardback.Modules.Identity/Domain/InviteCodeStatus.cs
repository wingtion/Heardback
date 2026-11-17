using System.Text.Json.Serialization;

namespace Heardback.Modules.Identity.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<InviteCodeStatus>))]
internal enum InviteCodeStatus
{
    Active,
    Used,
    Expired,
}
