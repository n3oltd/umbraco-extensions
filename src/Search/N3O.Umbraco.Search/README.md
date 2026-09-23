# N3O.Umbraco.Search

Generates and publishes the site's sitemap, and carries the search block a site places on a results
page.

The sitemap is assembled from every `ISitemapEntriesProvider` in our assemblies rather than from the
content tree alone, which is what lets a package add entries for things that are not Umbraco pages.
One provider here covers published content; other packages add their own.

Generation is a background job, triggered at application start and when content is published, so the
sitemap is a published artefact rather than something built per request. Entries are grouped into
sections derived from the content type, so a large site produces a sitemap index rather than one
oversized file.

It supplies no search implementation. A searcher package provides that, and the search block has
nothing to query without one.
