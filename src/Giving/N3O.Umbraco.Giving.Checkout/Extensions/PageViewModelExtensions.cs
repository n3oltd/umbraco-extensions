using Flurl;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Cart;
using N3O.Umbraco.Giving.Cart.Content;
using N3O.Umbraco.Pages;

namespace N3O.Umbraco.Giving.Checkout.Extensions;

public static class PageViewModelExtensions {
    public static string GetCartUrl(this IPageViewModel pageViewModel, IContentCache contentCache) {
        var cartUrl = contentCache.Single<CartPageContent>()?.Content()?.AbsoluteUrl();

        if (!cartUrl.HasValue()) {
            return null;
        }

        var url = new Url(cartUrl);

        if (pageViewModel.Content.IsCheckoutPage()) {
            url.SetQueryParam(CartConstants.QueryString.CheckoutView, 1);
        }

        return url;
    }
}
