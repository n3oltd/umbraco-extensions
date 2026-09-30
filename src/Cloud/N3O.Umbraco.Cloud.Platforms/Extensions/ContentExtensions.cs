using N3O.Umbraco.Cloud.Platforms.Content;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class ContentExtensions {
    public static Guid? GetCrowdfunderCampaignKey(this IContent content) {
        var alias = PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Properties.Campaign;
        var value = content.GetValue<string>(alias);

        if (UdiParser.TryParse(value, out GuidUdi udi)) {
            return udi.Guid;
        }

        return null;
    }

    public static bool IsCampaign(this IContent content, IContentTypeService contentTypeService) {
        return HasComposition(contentTypeService, content, AliasHelper<CampaignContent>.ContentTypeAlias());
    }

    public static bool IsCrossSell(this IContent content, IContentTypeService contentTypeService) {
        return HasComposition(contentTypeService, content, AliasHelper<CrossSellContent>.ContentTypeAlias());
    }

    public static bool IsCrowdfunder(this IContent content) {
        return content.ContentType.Alias == PlatformsConstants.CrowdfundingCampaigns.CrowdfundingCampaign.Alias;
    }

    public static bool IsFeed(this IContent content) {
        return content.ContentType.Alias == PlatformsConstants.Feeds.Feed.Alias;
    }

    public static bool IsFeeds(this IContent content) {
        return content.ContentType.Alias == PlatformsConstants.Feeds.Alias;
    }

    public static bool IsFeedItem(this IContent content, IContentTypeService contentTypeService) {
        return HasComposition(contentTypeService, content, PlatformsConstants.Feeds.Item.Alias);
    }

    public static bool IsOffering(this IContent content, IContentTypeService contentTypeService) {
        return HasComposition(contentTypeService, content, AliasHelper<OfferingContent>.ContentTypeAlias());
    }

    public static bool IsQurbaniSeasonContent(this IContent content) {
        return content.ContentType.Alias.EqualsInvariant(PlatformsConstants.Qurbani.Season.Alias);
    }
    
    public static bool IsQurbaniSeasonCategoryContent(this IContent content) {
        return content.ContentType.Alias.EqualsInvariant(PlatformsConstants.Qurbani.Season.Category.Alias);
    }

    public static bool IsZakatCalculatorSettings(this IContent content) {
        return content.ContentType.Alias.EqualsInvariant(PlatformsConstants.Zakat.Settings.Calculator.Alias);
    }

    public static bool IsZakatCalculatorSection(this IContent content) {
        return content.ContentType.Alias.EqualsInvariant(PlatformsConstants.Zakat.Settings.Calculator.Section.Alias);
    }

    public static bool IsZakatCalculatorField(this IContent content) {
        return content.ContentType.Alias.EqualsInvariant(PlatformsConstants.Zakat.Settings.Calculator.Field.Alias);
    }

    private static bool HasComposition(IContentTypeService contentTypeService,
                                       IContent content,
                                       string compositionAlias) {
        var contentType = contentTypeService.Get(content.ContentTypeId);

        return contentType.CompositionAliases().Contains(compositionAlias, true);
    }
}
