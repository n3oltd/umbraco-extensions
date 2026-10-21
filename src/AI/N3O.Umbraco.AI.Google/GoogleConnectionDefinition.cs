using N3O.Umbraco.AI.Connections;
using Umbraco.AI.Google;

namespace N3O.Umbraco.AI.Google;

public class GoogleConnectionDefinition : AIConnectionDefinition {
    public GoogleConnectionDefinition()
        : base("google",
               "Google",
               "google",
               "Google",
               [new AIConnectionSetting(nameof(GoogleProviderSettings.ApiKey), isSecret: true, isRequired: true)]) { }
}
