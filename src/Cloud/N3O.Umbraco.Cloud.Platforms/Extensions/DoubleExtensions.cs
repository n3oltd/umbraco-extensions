using N3O.Umbraco.Cloud.Platforms.Clients;
using PlatformsCurrency = N3O.Umbraco.Cloud.Platforms.Clients.Currency;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class DoubleExtensions {
    public static MoneyReq ToMoneyReq(this double amount, PlatformsCurrency? currency) {
        var money = new MoneyReq();
        money.Amount = amount;
        money.Currency = currency;

        return money;
    }
}
