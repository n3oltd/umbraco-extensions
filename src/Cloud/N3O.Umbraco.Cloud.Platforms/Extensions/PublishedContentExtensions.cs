using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class PublishedContentExtensions {
    public static IEnumerable<T> GetDescendantsOfCompositionTypeAs<T>(this IPublishedContent content) {
        return content.Descendants().Where(x => x.IsComposedOf(AliasHelper<T>.ContentTypeAlias())).As<T>();
    }

    public static bool IsPlatformsPage(this IPublishedContent content, IContentCache contentCache) {
        if (content == null) {
            return false;
        }

        return PlatformsSpecialPages.All.Any(x => content.Id == contentCache.Special(x)?.Id);
    }
}