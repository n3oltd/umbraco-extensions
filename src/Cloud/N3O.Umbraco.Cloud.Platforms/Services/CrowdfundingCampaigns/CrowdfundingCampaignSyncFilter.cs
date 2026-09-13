using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Sync;

namespace N3O.Umbraco.Cloud.Platforms;

public class CrowdfundingCampaignSyncFilter : ISyncFilter {
    public bool IsFilter(string contentTypeAlias) {
        return contentTypeAlias.EqualsInvariant(AliasHelper<CrowdfundingCampaignContent>.ContentTypeAlias());
    }

    public bool ShouldImport(string propertyAlias) {
        var stampAlias = AliasHelper<CrowdfundingCampaignContent>.PropertyAlias(x => x.ContentSyncStamp);

        return !propertyAlias.EqualsInvariant(stampAlias);
    }
}
