using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Extensions;

namespace N3O.Umbraco.Hosting;

public class StaticFileCachePolicy {
    private const string UmbracoCacheBusterKey = "umb__rnd";
    private const string VersionKey = "v";

    public void Apply(StaticFileResponseContext context) {
        var request = context.Context.Request;
        var response = context.Context.Response;

        if (IsCurrentVersion(request)) {
            response.Headers.CacheControl = "public, max-age=31536000, immutable";
        } else if (!request.Query.ContainsKey(UmbracoCacheBusterKey)) {
            response.Headers.CacheControl = "no-cache";
        }
    }

    private bool IsCurrentVersion(HttpRequest request) {
        var version = (string) request.Query[VersionKey];

        if (version.HasValue()) {
            var fileVersionProvider = request.HttpContext.RequestServices.GetRequiredService<IFileVersionProvider>();
            var versionedPath = fileVersionProvider.AddFileVersionToPath(request.PathBase, request.Path.Value);

            return versionedPath == QueryHelpers.AddQueryString(request.Path.Value, VersionKey, version);
        } else {
            return false;
        }
    }
}
