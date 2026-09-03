using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Content;
using System;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms.Content;

[UmbracoContent(PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias)]
public class CrowdfunderContent : UmbracoContent<CrowdfunderContent> {
    public Guid Key => Content().Key;

    public Campaign Campaign => GetValue(x => x.Campaign);
}
