using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public interface ICrowdfundingCampaignContentCopier {
    void CopyFromCampaign(IContent crowdfundingCampaign, IContent campaign);
}
