# N3O.Umbraco.Cloud.Platforms.Search

Connects a site's sitemap and search to content the cloud platform owns: it puts campaign offerings
into the sitemap, and resolves the names of the search collections the platform indexes.

Offerings are not Umbraco pages, so the generic content provider does not see them; this package
reads the campaigns the cloud has published for the subscription and emits a sitemap entry per
visible offering.

Visibility is filtered rather than assumed: an offering its campaign does not currently expose is
left out. The published campaigns file is expected to exist for every subscription, so a missing or
unreadable file raises rather than quietly producing a sitemap with no offerings in it.

Regeneration is triggered by campaign and offering webhooks rather than by Umbraco publishing, and
is scheduled a minute out so a burst of edits results in one rebuild.

The platform indexes its own content into collections it names, and publishes those names in the
subscription's infrastructure file. `IPlatformsCollectionNameResolver` reads a collection's name
from that file, so a site searching platform content never composes the name itself. The names are
per subscription rather than per environment, so staging and production search the same platform
collections. A missing or unreadable file raises, as does a file without the requested collection.
