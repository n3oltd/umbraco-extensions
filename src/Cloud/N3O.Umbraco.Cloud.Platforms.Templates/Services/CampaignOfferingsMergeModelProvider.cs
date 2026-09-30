using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Templates;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms.Templates;

public class CampaignOfferingsMergeModelProvider : MergeModelsProvider {
    private readonly ICampaignIdAccessor _campaignIdAccessor;
    private readonly ICdnClient _cdnClient;

    public CampaignOfferingsMergeModelProvider(ICampaignIdAccessor campaignIdAccessor, ICdnClient cdnClient) {
        _campaignIdAccessor = campaignIdAccessor;
        _cdnClient = cdnClient;
    }

    public override Task<bool> IsProviderForAsync(IPublishedContent content) => Task.FromResult(true);

    protected override async Task PopulateModelsAsync(IPublishedContent content,
                                                      Dictionary<string, object> mergeModels,
                                                      CancellationToken cancellationToken = default) {
        var campaignId = await _campaignIdAccessor.GetIdAsync(content, cancellationToken);

        if (campaignId.HasValue()) {
            var campaign = await _cdnClient.DownloadPublishedContentAsync<PublishedCampaign>(PublishedFileKinds.Campaign,
                                                                                             $"{campaignId}.json",
                                                                                             JsonSerializers.JsonProvider,
                                                                                             cancellationToken);

            mergeModels[PlatformsTemplateConstants.ModelKeys.CampaignOfferings] = campaign.Content.OrEmpty(x => x.Offerings);
        }
    }
}