# N3O.Umbraco.GeoIP.MaxMind

Locates a visitor by calling the MaxMind GeoIP2 web service with their IP address.

The account identifier and licence key come from Umbraco settings content. Because the service is
billed per query, results are held in a bounded in-memory cache keyed by IP address for twelve
hours; the bound matters, since an unbounded cache keyed by visitor address grows for as long as the
process lives. A failed lookup is reported as a failure rather than thrown, so a visitor is never
refused a page because geolocation was unavailable.
