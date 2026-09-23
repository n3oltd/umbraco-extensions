# N3O.Umbraco.Marketing

Exposes the analytics Umbraco Engage has collected over an API, so traffic, pageviews, sessions and
goal completions can be pulled out of a site per day and per configured site.

It also replaces two of Engage's own extractors. The visitor's IP address and the location derived
from it are taken from the values the proxy supplies rather than from the connection, which is what
a site behind a proxy has to do for Engage to record anything but the proxy's own address.

The export endpoints are authorised by a key held in Umbraco settings content and presented as a
header, not by back office authentication, because the caller is a machine. A request for a site
with no recorded pageviews is answered distinctly from one with none in the requested range, so a
misconfigured host can be told apart from a quiet week.

A Contentment data source lists Engage's segments, so an editor can pick one in a property rather
than typing its identifier.
