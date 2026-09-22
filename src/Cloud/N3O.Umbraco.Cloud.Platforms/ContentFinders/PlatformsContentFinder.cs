using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Routing;

namespace N3O.Umbraco.Cloud.Platforms.ContentFinders;

public class PlatformsContentFinder : IContentFinder {
    private readonly IPlatformsPageAccessor _platformsPageAccessor;
    private readonly IContentCache _contentCache;
    private readonly ILogger<PlatformsContentFinder> _logger;

    public PlatformsContentFinder(IPlatformsPageAccessor platformsPageAccessor,
                                  IContentCache contentCache,
                                  ILogger<PlatformsContentFinder> logger) {
        _platformsPageAccessor = platformsPageAccessor;
        _contentCache = contentCache;
        _logger = logger;
    }

    public async Task<bool> TryFindContent(IPublishedRequestBuilder request) {
        var found = false;

        var getPageResult = await _platformsPageAccessor.GetAsync();

        if (getPageResult.HasValue(x => x.Redirect)) {
            if (getPageResult.Redirect.Temporary) {
                request.SetRedirect(getPageResult.Redirect.UrlOrPath);
            } else {
                request.SetRedirectPermanent(getPageResult.Redirect.UrlOrPath);
            }
        } else if (getPageResult.HasValue(x => x.Page)) {
            var specialPage = GetSpecialPage(getPageResult.Page.Kind);
            var content = specialPage.HasValue() ? _contentCache.Special(specialPage) : null;

            if (content.HasValue()) {
                request.SetPublishedContent(content);

                found = true;
            } else if (specialPage.HasValue()) {
                _logger.LogError("The CDN has the {Kind} page at {Path} but the URL Settings {Picker} picker " +
                                 "is empty, so the page cannot be served",
                                 getPageResult.Page.Kind.Id,
                                 getPageResult.Page.Path,
                                 specialPage.UrlSettingsPropertyAlias);
            }
        }

        return found;
    }

    private SpecialContent GetSpecialPage(PublishedFileKind kind) {
        if (kind == PublishedFileKinds.CampaignPage) {
            return PlatformsSpecialPages.Campaign;
        } else if (kind == PublishedFileKinds.CrowdfunderPage) {
            return PlatformsSpecialPages.Crowdfunder;
        } else if (kind == PublishedFileKinds.CrowdfundingCampaignPage) {
            return PlatformsSpecialPages.CrowdfundingCampaign;
        } else if (kind == PublishedFileKinds.OfferingPage) {
            return PlatformsSpecialPages.Offering;
        } else {
            return null;
        }
    }
}