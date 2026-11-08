using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class AnalyticsField : NamedLookup {
    public AnalyticsField(string id, string name, string description, string key) : base(id, name) {
        Description = description;
        Key = key;
    }

    public string Description { get; }
    public string Key { get; }
}
