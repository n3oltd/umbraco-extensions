using N3O.Umbraco.Cloud.Platforms.Extensions;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.OpenGraph;
using N3O.Umbraco.Utilities;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsPageOpenGraphProvider : IOpenGraphProvider {
    private readonly IContentCache _contentCache;
    private readonly Lazy<IPlatformsPageAccessor> _platformsPageAccessor;
    private readonly IUrlBuilder _urlBuilder;

    public PlatformsPageOpenGraphProvider(IContentCache contentCache,
                                          Lazy<IPlatformsPageAccessor> platformsPageAccessor,
                                          IUrlBuilder urlBuilder) {
        _contentCache = contentCache;
        _platformsPageAccessor = platformsPageAccessor;
        _urlBuilder = urlBuilder;
    }

    public async Task AddOpenGraphAsync(IOpenGraphBuilder builder, IPublishedContent page) {
        var getPageResult = await _platformsPageAccessor.Value.GetAsync();

        if (getPageResult.HasValue(x => x.Page)) {
            builder.WithTitle(getPageResult.Page.Title);
            builder.WithUrl(getPageResult.Page.AbsoluteUrl(_urlBuilder));
        }
    }

    public bool IsProviderFor(IPublishedContent page) {
        return page.IsPlatformsPage(_contentCache);
    }
}
