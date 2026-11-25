using HeyRed.Mime;
using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Extensions;
using System;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Hosting;

public class PlatformsJsMiddleware : IMiddleware {
    private const string PlatformsJsPath = $"/{PlatformsConstants.PlatformsJs.Folder}/";

    private readonly ICdnClient _cdnClient;

    public PlatformsJsMiddleware(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        var requestPath = context.Request.Path.Value;

        if (requestPath != null &&
            !requestPath.Contains("..") &&
            requestPath.StartsWith(PlatformsJsPath, StringComparison.Ordinal)) {
            var platformsPath = requestPath.Substring(1);

            var content = await _cdnClient.DownloadPlatformsAsync(platformsPath);

            if (content.HasValue()) {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
                var method = context.Request.Method;

                if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method)) {
                    context.Response.ContentType = MimeTypesMap.GetMimeType(platformsPath);
                    context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                    context.Response.ContentLength = contentBytes.Length;

                    if (HttpMethods.IsGet(method)) {
                        await context.Response.Body.WriteAsync(contentBytes, 0, contentBytes.Length);
                    }
                }
            } else {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        } else {
            await next(context);
        }
    }
}
