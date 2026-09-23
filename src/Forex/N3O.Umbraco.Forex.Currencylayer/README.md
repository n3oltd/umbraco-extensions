# N3O.Umbraco.Forex.Currencylayer

Supplies exchange rates to `N3O.Umbraco.Forex` from the Currencylayer API. It is one of the
interchangeable rate providers; install exactly one.

The API key comes from Umbraco settings content and is appended to each request by a handler rather
than passed by the caller. The same settings carry a market rate adjustment, a percentage taken off
the rate Currencylayer returns before it is used, so a site can hold back a margin against currency
movement. It is a deduction, not a spread, and it applies in whichever direction the rate is used;
the value is clamped to between nought and a hundred, so a hundred converts every amount to nothing
rather than being rejected.
