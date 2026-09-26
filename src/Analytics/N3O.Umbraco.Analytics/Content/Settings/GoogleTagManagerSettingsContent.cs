using N3O.Umbraco.Content;

namespace N3O.Umbraco.Analytics.Content;

public class GoogleTagManagerSettingsContent : UmbracoContent<GoogleTagManagerSettingsContent> {
    public string ContainerId => GetValue(x => x.ContainerId);
    public string TagGatewayPath => GetValue(x => x.TagGatewayPath);
}
