namespace N3O.Umbraco.AI.Connections;

public class AIConnectionSetting {
    public AIConnectionSetting(string field, bool isSecret, bool isRequired) {
        Field = field;
        IsSecret = isSecret;
        IsRequired = isRequired;
    }

    public string Field { get; }
    public bool IsSecret { get; }
    public bool IsRequired { get; }

    public string GetConfigurationKey(string keyPrefix) {
        if (IsSecret) {
            return $"{AIConstants.ConfigurationSections.Secrets}:{keyPrefix}{Field}";
        } else {
            return $"{AIConstants.ConfigurationSections.Variables}:{keyPrefix}{Field}";
        }
    }
}
