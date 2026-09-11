using Flurl;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class UriExtensions {
    public static string RebaseOnSiteRoot(this Uri url, IUrlBuilder urlBuilder) {
        if (!url.HasValue()) {
            return null;
        }

        var rootUrl = urlBuilder.Root();

        if (rootUrl == null) {
            return null;
        }

        // Flurl's Url constructor reads a leading // as an authority, so the path is split and
        // assigned rather than parsed
        var pathAndQuery = url.IsAbsoluteUri ? url.AbsolutePath : url.OriginalString;
        var queryIndex = pathAndQuery.IndexOf('?');

        var rebasedUrl = new Url();
        rebasedUrl.Scheme = rootUrl.Scheme;
        rebasedUrl.Host = rootUrl.Host;
        rebasedUrl.Port = rootUrl.Port;
        rebasedUrl.Path = queryIndex == -1 ? pathAndQuery : pathAndQuery.Substring(0, queryIndex);

        if (queryIndex != -1) {
            rebasedUrl.Query = pathAndQuery.Substring(queryIndex + 1);
        }

        return rebasedUrl;
    }
}
