using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using N3O.Umbraco.Templates;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms.Templates;

public class CrowdfunderCampaignPageMergeModelProvider : MergeModelsProvider {
    private readonly ICampaignAccessor _campaignAccessor;
    private readonly ICdnClient _cdnClient;
    private readonly IJsonProvider _jsonProvider;
    private readonly IPlatformsPageAccessor _platformsPageAccessor;

    public CrowdfunderCampaignPageMergeModelProvider(ICampaignAccessor campaignAccessor,
                                                     ICdnClient cdnClient,
                                                     IJsonProvider jsonProvider,
                                                     IPlatformsPageAccessor platformsPageAccessor) {
        _campaignAccessor = campaignAccessor;
        _cdnClient = cdnClient;
        _jsonProvider = jsonProvider;
        _platformsPageAccessor = platformsPageAccessor;
    }

    public override Task<bool> IsProviderForAsync(IPublishedContent content) => Task.FromResult(true);

    protected override async Task PopulateModelsAsync(IPublishedContent content,
                                                      Dictionary<string, object> mergeModels,
                                                      CancellationToken cancellationToken = default) {
        var getPageResult = await _platformsPageAccessor.GetAsync(cancellationToken);
        var page = getPageResult?.Page;

        if (page?.Kind != PublishedFileKinds.CrowdfunderPage) {
            return;
        }

        var campaign = await _campaignAccessor.GetAsync(content, cancellationToken);

        if (campaign == null) {
            return;
        }

        var getCampaignPageResult = await _cdnClient.DownloadPlatformsPageAsync(_jsonProvider,
                                                                 PublishedFileKinds.CrowdfundingCampaignPage,
                                                                 page.Parent,
                                                                 campaign.Slug,
                                                                 cancellationToken);

        if (getCampaignPageResult.HasValue(x => x.Page)) {
            mergeModels[PlatformsTemplateConstants.ModelKeys.CrowdfunderCampaignPage] = getCampaignPageResult.Page.Content;
        }
    }
}
