using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Extensions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace N3O.Umbraco.Bundling.Middleware;

public class SourceMapsMiddleware {
    private readonly RequestDelegate _next;
    private readonly PathString _assetsPath;

    public SourceMapsMiddleware(RequestDelegate next, BundlingSettings settings) {
        _next = next;
        _assetsPath = GetAssetsPath(settings);
    }

    public async Task InvokeAsync(HttpContext context) {
        var path = context.Request.Path;

        if (path.HasValue &&
            path.Value.EndsWith(".map", StringComparison.OrdinalIgnoreCase) &&
            IsUnderAssetsPath(path)) {
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            return;
        }

        await _next(context);
    }

    private bool IsUnderAssetsPath(PathString path) {
        // The web root is held as an empty path because StartsWithSegments("/") matches nothing below it.
        if (!_assetsPath.HasValue) {
            return true;
        }

        return path.StartsWithSegments(_assetsPath);
    }

    private static PathString GetAssetsPath(BundlingSettings settings) {
        var directory = Path.GetDirectoryName(settings.ManifestPath) ?? string.Empty;
        var normalised = directory.Replace('\\', '/').Trim('/');

        if (!normalised.HasValue()) {
            return PathString.Empty;
        }

        return new PathString($"/{normalised}");
    }
}
