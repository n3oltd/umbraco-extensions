using Microsoft.Extensions.Logging;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Giving.Checkout.Controllers;

internal static class CheckoutRedirects {
    public static string DonateUrl(IContentCache contentCache, ILogger logger) {
        var donatePage = contentCache.Special(SpecialPages.Donate);

        if (donatePage != null) {
            return donatePage.AbsoluteUrl();
        }

        var homePage = contentCache.Special(SpecialPages.Home);

        logger.LogError("Could not resolve the {SpecialPage} special page, redirecting to {Fallback} instead",
                        SpecialPages.Donate.Id,
                        homePage == null ? "/" : SpecialPages.Home.Id);

        return homePage?.AbsoluteUrl() ?? "/";
    }
}
