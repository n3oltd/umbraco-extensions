# N3O.Umbraco.Blazor

Hosts server-side Blazor inside an Umbraco site. `AddBlazor` registers the Blazor server services and
the composer maps the Blazor hub into the Umbraco pipeline, so components run without a separate
host.

Blazor asks for its framework assets from paths rooted at `/_blazor/` and `/_content/`. Under
Umbraco those requests can arrive prefixed by the page's own route, which would not match, so
middleware detects the prefixed form and redirects it to the site root.
