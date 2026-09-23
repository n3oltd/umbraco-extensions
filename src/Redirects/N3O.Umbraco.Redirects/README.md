# N3O.Umbraco.Redirects

Serves redirects from two sources through one rewrite rule. Static redirects are declared in code at
startup, and Umbraco redirects come from redirect content nodes an editor creates, each pointing at
either a content item or an external URL and marked permanent or temporary.

Editor-managed redirects are held in memory and rebuilt into that store, not read per request. The
rebuild is queued as a background job when a redirect node is published and runs again at
application start, so a newly published redirect takes effect once the job has run rather than
immediately.

Paths are matched with leading and trailing slashes stripped and without regard to case, so the
several ways of writing the same path all match one entry. An external target is used as given; a
target that is not an absolute URL is treated as a site-relative path.
