# N3O.Umbraco.Cloud.Platforms.Search

Puts campaign offerings into the site's sitemap. Offerings are not Umbraco pages, so the generic
content provider does not see them; this package reads the campaigns the cloud has published for the
subscription and emits a sitemap entry per visible offering.

Visibility is filtered rather than assumed: an offering its campaign does not currently expose is
left out. The published campaigns file is expected to exist for every subscription, so a missing or
unreadable file raises rather than quietly producing a sitemap with no offerings in it.

Regeneration is triggered by campaign and offering webhooks rather than by Umbraco publishing, and
is scheduled a minute out so a burst of edits results in one rebuild.
