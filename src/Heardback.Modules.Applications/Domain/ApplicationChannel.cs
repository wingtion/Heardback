using System.Text.Json.Serialization;
using Heardback.Shared.Json;

namespace Heardback.Modules.Applications.Domain;

/// <summary>How the user applied (PLAN.md data model).</summary>
[JsonConverter(typeof(StrictStringEnumConverter<ApplicationChannel>))]
internal enum ApplicationChannel
{
    LinkedInEasyApply,
    CompanySite,
    Referral,
    DirectMessage,
    KariyerNet,
    Other,
}
