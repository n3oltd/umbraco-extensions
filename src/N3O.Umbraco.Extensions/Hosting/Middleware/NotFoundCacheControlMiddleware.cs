using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;

namespace N3O.Umbraco.Hosting;

public class NotFoundCacheControlMiddleware : IMiddleware {
    public async Task InvokeAsync(HttpContext context, RequestDelegate next) {
        context.Response.OnStarting(() => PreventCaching(context.Response));

        await next(context);
    }

    private Task PreventCaching(HttpResponse response) {
        if (response.StatusCode == StatusCodes.Status404NotFound &&
            !response.Headers.ContainsKey(HeaderNames.CacheControl)) {
            response.Headers.CacheControl = "no-store";
        }

        return Task.CompletedTask;
    }
}
