using Umbraco.Cms.Core.PropertyEditors;

namespace N3O.Umbraco.Content;

// Umbraco's own key regeneration for a copied node is only reachable through this handler's protected member.
public class NestedContentKeys : NestedContentPropertyHandler {
    public string Regenerate(string json) {
        return FormatPropertyValue(json, false);
    }
}
