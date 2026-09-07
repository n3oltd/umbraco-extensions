using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using Descriptions = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Descriptions;
using Folders = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Folders;
using Names = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Names;
using Tabs = N3O.Umbraco.Cloud.Platforms.PlatformsSchemaConstants.Tabs;

namespace N3O.Umbraco.Cloud.Platforms;

public partial class PlatformsSchemaComponent {
    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private const string LegacyCampaignDescription = "The campaign this crowdfunder raises for";

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private const string LegacyContainerAlias = "platformsCrowdfunders";

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private const string LegacyContainerName = "Crowdfunders";

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private const string LegacyFolder = "Crowdfunders";

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private const string LegacyItemAlias = "platformsCrowdfunder";

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private const string LegacyItemName = "Crowdfunder";

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private static readonly IReadOnlyList<Guid> ContainerKeys =
        GetSeededKeys(PlatformsConstants.CrowdfundingCampaigns.Alias, LegacyContainerAlias);

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private static readonly IReadOnlyList<Guid> ItemKeys =
        GetSeededKeys(PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias, LegacyItemAlias);

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private static readonly string[] LegacyCrowdfunderPageTemplateTabs = [
        "newCrowdfunderTemplate",
        "fundraisingPageTemplate"
    ];

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private static readonly string[] LegacyCrowdfundingCampaignTabs = ["Crowdfunder"];

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private readonly IContentTypeService _contentTypeService;

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private readonly IShortStringHelper _shortStringHelper;

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private void CorrectCrowdfundingCampaignNames() {
        var corrected = new[] { TryCorrect(CorrectItem), TryCorrect(CorrectContainer), TryCorrect(CorrectFolder) }
                        .Where(x => x.HasValue())
                        .ToList();

        if (corrected.Any()) {
            _logger.LogInformation("Corrected the crowdfunding campaign names of {Corrected}",
                                   string.Join(", ", corrected));
        }
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private bool CorrectCampaignDescription(IContentType item) {
        var alias = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Properties.Campaign;
        var campaign = item.PropertyTypes.FirstOrDefault(x => x.Alias == alias);

        if (campaign == null || !campaign.Description.EqualsInvariant(LegacyCampaignDescription)) {
            return false;
        }

        campaign.Description = Descriptions.CrowdfundingCampaignCampaign;

        return true;
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private string CorrectContainer() {
        var container = FindSeeded(ContainerKeys);

        if (container == null || !CorrectName(container, LegacyContainerName, Names.CrowdfundingCampaigns)) {
            return null;
        }

        _contentTypeService.Save(container);

        return container.Alias;
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private string CorrectFolder() {
        var platforms = _contentTypeService.GetContainers(Folders.Platforms, 1).SingleOrDefault();

        if (platforms == null) {
            return null;
        }

        var folders = _contentTypeService.GetContainers(Folders.CrowdfundingCampaigns, platforms.Level + 1)
                                         .Concat(_contentTypeService.GetContainers(LegacyFolder, platforms.Level + 1))
                                         .Where(x => x.ParentId == platforms.Id)
                                         .ToList();

        var folder = folders.SingleOrDefault(x => x.Name.EqualsInvariant(LegacyFolder));

        if (folder == null || folders.Any(x => x.Name.EqualsInvariant(Folders.CrowdfundingCampaigns))) {
            return null;
        }

        var attempt = _contentTypeService.RenameContainer(folder.Id, Folders.CrowdfundingCampaigns);

        if (!attempt.Success) {
            _logger.LogWarning(attempt.Exception,
                               "Could not rename the {Folder} content type folder ({Result})",
                               LegacyFolder,
                               attempt.Result?.Result);

            return null;
        }

        return Folders.CrowdfundingCampaigns;
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private string CorrectItem() {
        var item = FindSeeded(ItemKeys);

        if (item == null) {
            return null;
        }

        var corrections = new[] {
            CorrectTab(item, LegacyCrowdfundingCampaignTabs, Tabs.CrowdfundingCampaign),
            CorrectTab(item, LegacyCrowdfunderPageTemplateTabs, Tabs.CrowdfunderPageTemplate),
            CorrectCampaignDescription(item),
            CorrectName(item, LegacyItemName, Names.CrowdfundingCampaign)
        };

        if (corrections.None(x => x)) {
            return null;
        }

        _contentTypeService.Save(item);

        return item.Alias;
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private bool CorrectName(IContentType contentType, string legacyName, string name) {
        if (!contentType.Name.EqualsInvariant(legacyName)) {
            return false;
        }

        contentType.Name = name;

        return true;
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private bool CorrectTab(IContentType contentType, IReadOnlyList<string> legacyAliases, string name) {
        var alias = name.ToSafeAlias(_shortStringHelper, true);

        if (contentType.PropertyGroups.Any(x => x.Alias.EqualsInvariant(alias))) {
            return false;
        }

        var tab = contentType.PropertyGroups
                             .FirstOrDefault(x => x.Type == PropertyGroupType.Tab &&
                                                  legacyAliases.Contains(x.Alias, true));

        if (tab == null) {
            return false;
        }

        var legacyAlias = tab.Alias;

        tab.Alias = alias;
        tab.Name = name;

        foreach (var group in contentType.PropertyGroups) {
            if (group.Alias.StartsWith($"{legacyAlias}/", StringComparison.InvariantCultureIgnoreCase)) {
                group.Alias = $"{alias}{group.Alias.Substring(legacyAlias.Length)}";
            }
        }

        return true;
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private IContentType FindSeeded(IReadOnlyList<Guid> seededKeys) {
        return seededKeys.Select(x => _contentTypeService.Get(x)).FirstOrDefault(x => x != null);
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private string TryCorrect(Func<string> correct) {
        try {
            return correct();
        } catch (Exception ex) {
            _logger.LogError(ex, "Could not correct the crowdfunding campaign names");

            return null;
        }
    }

    [Obsolete("Delete me once every site's crowdfunding campaign types carry the new names")]
    private static IReadOnlyList<Guid> GetSeededKeys(params string[] aliases) {
        return aliases.Select(x => UmbracoId.Deterministic(IdScope.ContentType, x)).ToList();
    }
}
