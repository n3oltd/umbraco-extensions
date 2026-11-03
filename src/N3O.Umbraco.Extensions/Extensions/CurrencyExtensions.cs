using N3O.Umbraco.Financial;
using System.Collections.Generic;
using System.Linq;

namespace N3O.Umbraco.Extensions;

public static class CurrencyExtensions {
    public static Currency FindByCode(this IEnumerable<Currency> currencies, string isoCode) {
        var currency = currencies.SingleOrDefault(x => x.Code.EqualsInvariant(isoCode));

        return currency;
    }

    public static ForexMoney ForexZero(this Currency currency) {
        return new ForexMoney(Zero(currency), Zero(currency), 1);
    }
    
    public static Money Zero(this Currency currency) {
        return new Money(0, currency);
    }
}
