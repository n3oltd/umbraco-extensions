# N3O.Umbraco.GeoIP

Defines the seam for locating a visitor from their IP address. `IIPGeoLocationProvider` returns a
country, city and region, or a failure, and a provider package implements it.

`UseGeoIPDefaultCurrencyProvider` builds on that to choose the currency a visitor is shown first,
taking the currency of the located country and falling back to the site's configured default when
the lookup fails or the country has no currency among those the site offers.
