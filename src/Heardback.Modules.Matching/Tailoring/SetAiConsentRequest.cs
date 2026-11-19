using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Matching.Tailoring;

internal sealed record SetAiConsentRequest([property: Required] bool? Allowed);
