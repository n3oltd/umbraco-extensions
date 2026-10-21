using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Concurrent;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.ContentFinders;

public static class SpecialContentPathParser {
    private static readonly ConcurrentDictionary<(SpecialContent, string), string> SpecialPaths = new();

    public static void Flush() {
        SpecialPaths.Clear();
    }

    public static string GetPath(IContentCache contentCache, SpecialContent specialContent, string culture = null) {
        var cacheKey = GetCacheKey(specialContent, culture);

        // An ambient resolution varies with the caller's context, so only an explicit culture's path is cached.
        if (culture.HasValue() && SpecialPaths.TryGetValue(cacheKey, out var cachedPath)) {
            return cachedPath;
        }

        var specialPage = contentCache.Special(specialContent);

        if (!specialPage.HasValue()) {
            return null;
        }

        var path = specialPage.RelativeUrl(culture).StripTrailingSlash();

        if (!IsRoutable(path)) {
            return null;
        }

        if (culture.HasValue()) {
            SpecialPaths.TryAdd(cacheKey, path);
        }

        return path;
    }

    public static string ParseUri(IContentCache contentCache,
                                  SpecialContent specialContent,
                                  Uri requestUri,
                                  string culture = null) {
        var specialPath = GetPath(contentCache, specialContent, culture);

        if (!specialPath.HasValue()) {
            return null;
        }

        var requestedPath = requestUri.GetAbsolutePathDecoded().ToLowerInvariant().StripTrailingSlash();

        if (requestedPath.StartsWith(specialPath, StringComparison.InvariantCultureIgnoreCase)) {
            return requestedPath.Substring(specialPath.Length).EnsureTrailingSlash();
        }

        return null;
    }

    private static (SpecialContent, string) GetCacheKey(SpecialContent specialContent, string culture) {
        return (specialContent, culture?.ToLowerInvariant());
    }

    private static bool IsRoutable(string path) {
        return path.HasValue() && path.StartsWith('/');
    }
}
