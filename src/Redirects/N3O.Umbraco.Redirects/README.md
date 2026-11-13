# N3O.Umbraco.Redirects

Serves redirects from two sources through one rewrite rule. Static redirects are declared in code at
startup, and Umbraco redirects come from redirect content nodes an editor creates, each pointing at
either a content item or an external URL and marked permanent or temporary.

Editor-managed redirects are held in memory and rebuilt into that store, not read per request. The
rebuild is queued as a background job when a redirect node is published and runs again at
application start, so a newly published redirect takes effect once the job has run rather than
immediately.

Paths are matched in their decoded form, with leading and trailing slashes stripped and without
regard to case, so the several ways of writing the same path all match one entry: a node named
`old page` or `old%20page` matches a request for `/old%20page`. Where two redirects normalise to the
same path, the last one added wins. An external target is used as given; a target that is not an
absolute URL is treated as a site-relative path. Characters a URL cannot carry, such as spaces or
non-ASCII letters, are percent-encoded in the target when the redirect is sent. The query string of the
request is passed on when the target is a site-relative path or an HTTP(S) URL, appended
to any query the target already has and placed before any fragment, except for parameters the target
sets itself, which keep the target's value.
