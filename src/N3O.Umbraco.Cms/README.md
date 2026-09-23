# N3O.Umbraco.Cms

The host entry point for a site built on these packages. `UmbracoCms.Run<TStartup>` builds the
generic host, configures Kestrel limits and hands control to a `CmsStartup` subclass, which adds
Umbraco with the back office, website and Delivery API enabled and discovers composers.

The prefix passed to `Run` is given to `OurAssemblies`, and it decides which assemblies the whole
convention-based registration mechanism will scan. A site whose own assemblies do not begin with
that prefix will compile and start, but none of its composers, content models or lookups will be
found.

`CmsStartup` exposes `ConfigureEndpoints`, `ConfigureMiddleware` and `ConfigureStaticFiles` for a
site to override, and serves readiness on `/healthz` and liveness on `/livez`, each filtered by the
health check tag of the same name.
