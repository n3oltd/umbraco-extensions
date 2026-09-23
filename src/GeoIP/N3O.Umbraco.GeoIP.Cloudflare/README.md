# N3O.Umbraco.GeoIP.Cloudflare

Locates a visitor from the geolocation headers Cloudflare adds to a proxied request, so no lookup
service is called and no per-request cost is incurred.

The headers only arrive on requests that actually passed through Cloudflare. When none of them is
present the lookup reports failure rather than guessing, which is what a site sees in local
development and behind any other proxy.
