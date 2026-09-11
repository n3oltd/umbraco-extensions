using Flurl;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Content;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.Extensions;

public static class PublishedContentExtensions {
    // TODO This is ugly, but is no different from what content.Url() call below is doing
    private static IUrlBuilder UrlBuilder { get; } = StaticServiceProvider.Instance.GetRequiredService<IUrlBuilder>();
    private static IContentCache ContentCache { get; } = StaticServiceProvider.Instance.GetRequiredService<IContentCache>();
    
    // We need to do all of this as for background jobs Umbraco picks up the URL from the context and ends up
    // resolving to localhost
    public static string AbsoluteUrl(this IPublishedContent content, string culture = null) {
        var url = new Url(content.Url(mode: UrlMode.Absolute, culture: culture));

        if (ContentCache.Single<UrlSettingsContent>() == null) {
            return url;
        }

        var rootUrl = UrlBuilder.Root();

        url.Scheme = rootUrl.Scheme;
        url.Host = rootUrl.Host;
        url.Port = rootUrl.Port;

        return url;
    }
    
    public static T As<T>(this IPublishedContent publishedContent, VariationContext variationContext = null) {
        if (publishedContent is T typedContent) {
            return typedContent;
        }

        if (publishedContent == null) {
            return default;
        }

        if (!typeof(T).ImplementsInterface<IUmbracoContent>()) {
            throw new Exception($"{typeof(T).GetFriendlyName()} does not implement {nameof(IUmbracoContent)}");
        }
    
        return ConvertTo<T>(publishedContent, variationContext);
    }

    public static IReadOnlyList<T> As<T>(this IEnumerable<IPublishedContent> publishedContents,
                                         VariationContext variationContext = null) {
        return publishedContents.Select(x => x.As<T>(variationContext)).ToList();
    }

    public static T Child<T>(this IPublishedContent content)
        where T : class, IPublishedContent {
        return Child<T>(content, _ => true);
    }

    public static T Child<T>(this IPublishedContent content, Func<T, bool> filter)
        where T : class, IPublishedContent {
        return Children(content, filter).FirstOrDefault();
    }

    public static IEnumerable<T> Children<T>(this IPublishedContent content)
        where T : class, IPublishedContent {
        return Children<T>(content, _ => true);
    }

    public static IEnumerable<T> Children<T>(this IPublishedContent content, Func<T, bool> filter)
        where T : class, IPublishedContent {
        return FriendlyPublishedContentExtensions.Children<T>(content).Where(filter);
    }

    public static T Descendant<T>(this IPublishedContent content) where T : class, IPublishedContent {
        return Descendant<T>(content, _ => true);
    }

    public static T Descendant<T>(this IPublishedContent content, Func<T, bool> filter)
        where T : class, IPublishedContent {
        return Descendants(content, filter).FirstOrDefault();
    }

    public static IEnumerable<T> Descendants<T>(this IPublishedContent content, Func<T, bool> filter)
        where T : class, IPublishedContent {
        return FriendlyPublishedContentExtensions.Descendants<T>(content).Where(filter);
    }

    public static bool HasTemplate(this IPublishedContent content) {
        return content.TemplateId != null && content.TemplateId > 0;
    }

    public static string RelativeUrl(this IPublishedContent content) {
        return content.Url(mode: UrlMode.Relative);
    }
    
    public static T To<T>(this IPublishedContent publishedContent, VariationContext variationContext = null)
        where T : IUmbracoContent, new() {
        return ConvertTo<T>(publishedContent, variationContext);
    }
    
    private static T ConvertTo<T>(this IPublishedContent publishedContent, VariationContext variationContext = null) {
        if (publishedContent == null) {
            return default;
        }
        
        var model = Activator.CreateInstance<T>();

        if (model is IUmbracoContent umbracoContent) {
            umbracoContent.SetContent(publishedContent);
            
            if (variationContext != null) {
                umbracoContent.SetVariationContext(variationContext);
            }
        }
        
        return model;
    }
}
