using N3O.Umbraco.AI.Connections;
using Umbraco.AI.MicrosoftFoundry;

namespace N3O.Umbraco.AI.MicrosoftFoundry;

public class MicrosoftFoundryConnectionDefinition : AIConnectionDefinition {
    public MicrosoftFoundryConnectionDefinition()
        : base("microsoft-foundry",
               "Microsoft AI Foundry",
               "microsoft-foundry",
               "MicrosoftFoundry",
               [new AIConnectionSetting(nameof(MicrosoftFoundryProviderSettings.ApiKey),
                                        isSecret: true,
                                        isRequired: true),
                new AIConnectionSetting(nameof(MicrosoftFoundryProviderSettings.Endpoint),
                                        isSecret: false,
                                        isRequired: true)]) { }
}
