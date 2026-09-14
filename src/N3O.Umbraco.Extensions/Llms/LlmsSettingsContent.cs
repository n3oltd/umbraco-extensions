using N3O.Umbraco.Content;

namespace N3O.Umbraco.Llms;

public class LlmsSettingsContent : UmbracoContent<LlmsSettingsContent> {
    public string Markdown => GetValue(x => x.Markdown);
}
