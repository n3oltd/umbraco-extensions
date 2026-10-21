using N3O.Umbraco.AI.Connections;
using Umbraco.AI.OpenAI;

namespace N3O.Umbraco.AI.OpenAI;

public class OpenAIConnectionDefinition : AIConnectionDefinition {
    public OpenAIConnectionDefinition()
        : base("openai",
               "OpenAI",
               "openai",
               "OpenAI",
               [new AIConnectionSetting(nameof(OpenAIProviderSettings.ApiKey), true, true),
                new AIConnectionSetting(nameof(OpenAIProviderSettings.Endpoint), false, false)]) { }
}
