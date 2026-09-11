using N3O.Umbraco.Blocks;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;
using CampaignProperties = N3O.Umbraco.Cloud.Platforms.PlatformsConstants.Campaigns.Properties;
using CrowdfundingCampaignProperties =
    N3O.Umbraco.Cloud.Platforms.PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Properties;

namespace N3O.Umbraco.Cloud.Platforms;

public class CrowdfundingCampaignContentCopier : ICrowdfundingCampaignContentCopier {
    private static readonly IReadOnlyList<(string Source, string[] Destinations)> Mappings =
        new[] {
            (CampaignProperties.HeroImage, new[] { CrowdfundingCampaignProperties.PageHeroImage,
                                                   CrowdfundingCampaignProperties.PageTemplateHeroImage }),
            (CampaignProperties.PageContent, new[] { CrowdfundingCampaignProperties.PageContent,
                                                     CrowdfundingCampaignProperties.PageTemplateContent }),
            (CampaignProperties.PageContentAdditional,
             new[] { CrowdfundingCampaignProperties.PageContentAdditional,
                     CrowdfundingCampaignProperties.PageTemplateContentAdditional })
        };

    private readonly IEnumerable<IBlocksCloner> _cloners;

    public CrowdfundingCampaignContentCopier(IEnumerable<IBlocksCloner> cloners) {
        _cloners = cloners;
    }

    public void CopyFromCampaign(IContent crowdfundingCampaign, IContent campaign) {
        foreach (var mapping in Mappings) {
            CopyProperty(crowdfundingCampaign, campaign, mapping.Source, mapping.Destinations);
        }
    }

    private void CopyProperty(IContent crowdfundingCampaign,
                              IContent campaign,
                              string sourceAlias,
                              IEnumerable<string> destinationAliases) {
        if (!campaign.HasProperty(sourceAlias)) {
            return;
        }

        var value = campaign.GetValue<string>(sourceAlias);

        if (!value.HasValue()) {
            return;
        }

        var sourceEditorAlias = campaign.Properties[sourceAlias].PropertyType.PropertyEditorAlias;

        foreach (var destinationAlias in destinationAliases) {
            if (!crowdfundingCampaign.HasProperty(destinationAlias)) {
                continue;
            }

            var destinationEditorAlias = crowdfundingCampaign.Properties[destinationAlias]
                                                             .PropertyType
                                                             .PropertyEditorAlias;

            // Sites define these properties, so a campaign and its crowdfunding page can differ in editor
            if (!sourceEditorAlias.EqualsInvariant(destinationEditorAlias)) {
                continue;
            }

            crowdfundingCampaign.SetValue(destinationAlias, Clone(destinationEditorAlias, value));
        }
    }

    private string Clone(string editorAlias, string value) {
        var cloner = _cloners.FirstOrDefault(x => x.CanClone(editorAlias));

        return cloner == null ? value : cloner.Clone(value);
    }
}
