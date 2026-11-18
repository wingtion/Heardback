using System.Text.Json.Serialization;

namespace Heardback.Modules.Applications.Domain;

/// <summary>How the user applied (PLAN.md data model).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApplicationChannel>))]
internal enum ApplicationChannel
{
    LinkedInEasyApply,
    CompanySite,
    Referral,
    DirectMessage,
    KariyerNet,
    Other,
}
