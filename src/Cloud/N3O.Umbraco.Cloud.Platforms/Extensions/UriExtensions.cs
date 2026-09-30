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

        var pathAndQuery = url.IsAbsoluteUri ? url.PathAndQuery : url.OriginalString;
        var queryIndex = pathAndQuery.IndexOf('?');
        var path = queryIndex == -1 ? pathAndQuery : pathAndQuery.Substring(0, queryIndex);

        var rebasedUrl = new Url(rootUrl.ToString());

        rebasedUrl.AppendPathSegments(path.Split('/', StringSplitOptions.RemoveEmptyEntries));

        if (queryIndex != -1) {
            rebasedUrl.Query = pathAndQuery.Substring(queryIndex + 1);
        }

        return rebasedUrl;
    }
}
