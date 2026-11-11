using HeyRed.Mime;
using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Extensions;
using System;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Hosting;

public class ConnectMiddleware : IMiddleware {
    private readonly ISubscriptionAccessor _subscriptionAccessor;
    private readonly ICdnClient _cdnClient;

    public ConnectMiddleware(ISubscriptionAccessor subscriptionAccessor, ICdnClient cdnClient) {
        _subscriptionAccessor = subscriptionAccessor;
        _cdnClient = cdnClient;
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        var requestPath = context.Request.Path.Value;
        var subscription = _subscriptionAccessor.GetSubscription();

        var connectPath = subscription.HasValue() ? $"/connect-{subscription.Id.Code}/" : null;
        
        if (requestPath != null &&
            connectPath != null &&
            !requestPath.Contains("..") &&
            requestPath.StartsWith(connectPath, StringComparison.InvariantCultureIgnoreCase)) {
            var cdnPath = requestPath.Substring(connectPath.Length);

            context.Response.Headers["X-Robots-Tag"] = "noindex";

            var content = await _cdnClient.DownloadAsync(cdnPath);

            if (content.HasValue()) {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
                var method = context.Request.Method;

                if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method)) {
                    context.Response.ContentType = MimeTypesMap.GetMimeType(cdnPath);
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