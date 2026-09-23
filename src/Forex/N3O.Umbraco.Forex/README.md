# N3O.Umbraco.Forex

Converts money between currencies. `IForexConverter` exposes the conversion in both directions, base
to quote and quote to base, so a caller states which side of the pair it holds rather than having to
know which way the rate is quoted.

Rates come from an `IExchangeRateProvider`, which this package does not implement; a provider
package supplies one. Rates are cached per day and per currency pair, first in memory and then in
the database, so a rate is fetched at most once a day for a pair and stays stable for the rest of
it. That is deliberate — a price quoted to a supporter should not move between the page and the
payment — but it does mean a rate cached from a bad response persists for the day.
