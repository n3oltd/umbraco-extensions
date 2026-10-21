using N3O.Umbraco.AI.Connections;
using Umbraco.AI.Anthropic;

namespace N3O.Umbraco.AI.Anthropic;

public class AnthropicConnectionDefinition : AIConnectionDefinition {
    public AnthropicConnectionDefinition()
        : base("anthropic",
               "Anthropic",
               "anthropic",
               "Anthropic",
               [new AIConnectionSetting(nameof(AnthropicProviderSettings.ApiKey), true, true),
                new AIConnectionSetting(nameof(AnthropicProviderSettings.Endpoint), false, false)]) { }
}
