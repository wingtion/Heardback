using Microsoft.Extensions.Options;

namespace Heardback.Infrastructure.Llm.Settings;

[OptionsValidator]
internal sealed partial class LlmSettingsValidator : IValidateOptions<LlmSettings>;
