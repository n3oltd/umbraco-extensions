using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.PageTitle;
using System;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsPageTitleProvider : IPageTitleProvider {
    private readonly IContentCache _contentCache;
    private readonly Lazy<IPlatformsPageAccessor> _platformsPageAccessor;

    public PlatformsPageTitleProvider(IContentCache contentCache,
                                      Lazy<IPlatformsPageAccessor> platformsPageAccessor) {
        _contentCache = contentCache;
        _platformsPageAccessor = platformsPageAccessor;
    }

    public string GetPageTitle(IPublishedContent page) {
        return GetPlatformsPageTitle();
    }

    public bool IsProviderFor(IPublishedContent page) {
        return page.IsPlatformsPage(_contentCache) && GetPlatformsPageTitle().HasValue();
    }

    private string GetPlatformsPageTitle() {
        return _platformsPageAccessor.Value.GetAsync().GetAwaiter().GetResult()?.Page?.Title;
    }
}