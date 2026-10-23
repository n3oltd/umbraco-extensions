using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using PlatformsCurrency = N3O.Umbraco.Cloud.Platforms.Clients.Currency;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class BaseCurrencyAccessorExtensions {
    public static PlatformsCurrency? GetPlatformsBaseCurrency(this IBaseCurrencyAccessor baseCurrencyAccessor) {
        return baseCurrencyAccessor.GetBaseCurrency().Code.ToEnum<PlatformsCurrency>();
    }
}
