# N3O.Umbraco.Cdn.Cloudflare

Integrates a site with Cloudflare. `ICloudflareStreams` uploads a video to Cloudflare Stream from a
URL and returns the playback details, and a remote IP address accessor reads the visitor's address
from the `CF-Connecting-IP` header, which is what a site behind Cloudflare has to read because the
connection's own address is Cloudflare's. It falls back to the connection address when that header
is absent or unparseable.

The Stream account identifier and API token come from Umbraco settings content. When either is
missing the Stream client is not constructed and resolving it yields null, so a site that does not
use Stream needs no configuration at all.
