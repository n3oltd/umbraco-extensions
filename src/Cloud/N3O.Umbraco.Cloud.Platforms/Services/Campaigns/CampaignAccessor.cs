using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Extensions;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms;

public class CampaignAccessor : ICampaignAccessor {
    private readonly ICampaignIdAccessor _campaignIdAccessor;
    private readonly ICdnClient _cdnClient;

    public CampaignAccessor(ICampaignIdAccessor campaignIdAccessor, ICdnClient cdnClient) {
        _campaignIdAccessor = campaignIdAccessor;
        _cdnClient = cdnClient;
    }
    
    public async Task<PublishedCampaign> GetAsync(IPublishedContent content, CancellationToken cancellationToken = default) {
        var campaignId = await _campaignIdAccessor.GetIdAsync(content, cancellationToken: cancellationToken);

        if (campaignId.HasValue()) {
            var result = await _cdnClient.DownloadPublishedContentAsync<PublishedCampaign>(PublishedFileKinds.Campaign,
                                                                                           $"{campaignId}.json",
                                                                                           JsonSerializers.JsonProvider,
                                                                                           cancellationToken);

            return result.Content;
        } else {
            return null;   
        }
    }
}
