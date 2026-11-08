using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using Umbraco.Cms.Web.Common.Routing;

namespace N3O.Umbraco.Security;

public class RenderFailures : IRenderFailures {
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RenderFailures(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool CanIsolate(Exception exception) {
        var httpContext = GetPageHttpContext();

        if (httpContext == null) {
            return false;
        }

        return exception is not OperationCanceledException || !httpContext.RequestAborted.IsCancellationRequested;
    }

    public IHtmlContent ToHtml(Exception exception) {
        var httpContext = GetPageHttpContext();

        if (httpContext == null) {
            return HtmlString.Empty;
        }

        var backofficeUser = httpContext.RequestServices.GetRequiredService<IBackofficeUser>();

        if (!backofficeUser.IsLoggedIn()) {
            return HtmlString.Empty;
        }

        var html = new HtmlContentBuilder();

        html.AppendHtml("<pre>");
        html.Append(exception.ToString());
        html.AppendHtml("</pre>");

        return html;
    }

    // HTML rendered outside a routed page, for a publish or a search index, is stored and served later, so its
    // failures are neither isolated nor shown.
    private HttpContext GetPageHttpContext() {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.Features.Get<UmbracoRouteValues>() == null) {
            return null;
        }

        return httpContext;
    }
}
