using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.Data.Filters;
using N3O.Umbraco.Data.Models;
using N3O.Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Data.Filters;

public class CrowdfundingCampaignImportPropertyFilter : IImportPropertyFilter {
    public bool CanImport(UmbracoPropertyInfo propertyInfo) {
        var stampAlias = AliasHelper<CrowdfundingCampaignContent>.PropertyAlias(x => x.ContentSyncStamp);

        return !propertyInfo.Type.Alias.EqualsInvariant(stampAlias);
    }

    public bool IsFilter(UmbracoPropertyInfo propertyInfo) {
        var contentTypeAlias = AliasHelper<CrowdfundingCampaignContent>.ContentTypeAlias();

        return propertyInfo.ContentType.Alias.EqualsInvariant(contentTypeAlias);
    }
}
