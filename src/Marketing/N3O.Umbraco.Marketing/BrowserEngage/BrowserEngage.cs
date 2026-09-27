using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;

namespace N3O.Umbraco.Marketing;

// With browser Engage on, a page is rendered identically for every visitor and registers its pageview from the
// browser, except a page Engage has to vary on the server, which is rendered as Engage renders it by default.
public static class BrowserEngage {
    private static readonly string ServerRenderedKey = $"{nameof(BrowserEngage)}.ServerRendered";

    public static bool IsEnabled() {
        return EnvironmentData.GetOurValue(MarketingConstants.Environment.Keys.BrowserEngage).EqualsInvariant("enabled");
    }

    public static bool IsServerRendered(HttpContext context) {
        return context.Items.ContainsKey(ServerRenderedKey);
    }

    public static void RenderOnServer(HttpContext context) {
        context.Items[ServerRenderedKey] = true;
    }
}
