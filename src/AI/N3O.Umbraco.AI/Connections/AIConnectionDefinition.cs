using System.Collections.Generic;

namespace N3O.Umbraco.AI.Connections;

public abstract class AIConnectionDefinition {
    protected AIConnectionDefinition(string alias,
                                     string name,
                                     string providerId,
                                     string keyPrefix,
                                     IReadOnlyList<AIConnectionSetting> settings) {
        Alias = alias;
        Name = name;
        ProviderId = providerId;
        KeyPrefix = keyPrefix;
        Settings = settings;
    }

    public string Alias { get; }
    public string Name { get; }
    public string ProviderId { get; }
    public string KeyPrefix { get; }
    public IReadOnlyList<AIConnectionSetting> Settings { get; }
}
