using Microsoft.Extensions.Logging;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Checkout.Content;
using N3O.Umbraco.Giving.Checkout.Lookups;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Giving.Checkout.Controllers;

internal static class CheckoutRedirects {
    public static string CompletePageUrl(IContentCache contentCache, ILogger logger) {
        var completePage = contentCache.Single<CheckoutCompletePageContent>();

        if (completePage != null) {
            return completePage.Content().AbsoluteUrl();
        }

        logger.LogError("Could not resolve {ContentType} content, redirecting to the {SpecialPage} page instead",
                        AliasHelper<CheckoutCompletePageContent>.ContentTypeAlias(),
                        SpecialPages.Donate.Id);

        return DonateUrl(contentCache, logger);
    }

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

    public static string StageUrl(CheckoutStage stage, IContentCache contentCache, ILogger logger) {
        var stageUrl = stage.GetUrl(contentCache);

        if (stageUrl.HasValue()) {
            return stageUrl;
        }

        logger.LogError("Could not resolve the page for the {Stage} checkout stage, redirecting to the {SpecialPage} page instead",
                        stage.Id,
                        SpecialPages.Donate.Id);

        return DonateUrl(contentCache, logger);
    }
}
