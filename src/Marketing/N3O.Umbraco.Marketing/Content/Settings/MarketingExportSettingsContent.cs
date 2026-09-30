using N3O.Umbraco.Content;

namespace N3O.Umbraco.Marketing.Content;

public class MarketingExportSettingsContent : UmbracoContent<MarketingExportSettingsContent> {
    public string ExportKey => GetValue(x => x.ExportKey);
}
