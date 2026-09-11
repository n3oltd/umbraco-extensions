using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace N3O.Umbraco.Cloud.Platforms;

public interface ICrowdfundingCampaignContentCopier {
    void CopyFromCampaign(IContent crowdfundingCampaign, IContent campaign);
    IReadOnlyDictionary<string, string> GetUpdatesFromCampaign(IContent crowdfundingCampaign, IContent campaign);
}
