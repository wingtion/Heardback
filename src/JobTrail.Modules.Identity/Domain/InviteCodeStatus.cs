using System.Text.Json.Serialization;

namespace JobTrail.Modules.Identity.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<InviteCodeStatus>))]
internal enum InviteCodeStatus
{
    Active,
    Used,
    Expired,
}
