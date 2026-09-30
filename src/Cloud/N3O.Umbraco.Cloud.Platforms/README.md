# N3O.Umbraco.Cloud.Platforms

Gives editors the content types behind the giving platform — campaigns, offerings, cross-sells,
crowdfunding campaigns, donation form content and state, Qurbani seasons and the Zakat calculator —
and publishes what they edit to the cloud, so the front end reads it from the platform rather than
from Umbraco.

Publication is event-driven. Saving, publishing, unpublishing or deleting one of these content items
raises a notification whose handler builds the corresponding request and queues it as a background
job, so an editor's save is not held up by the API call and a failed call is retried by the
scheduler rather than lost.

The content types and data types themselves are seeded by a component at startup, which audits what
is present and creates what is missing. It only runs when the feature is enabled, so a site that
does not use the platform is not given the schema.

```json
{
  "Platforms": {
    "Enabled": true,
    "MediaUrl": "https://<the site's public origin>"
  }
}
```

`Enabled` gates the schema seeding described above, not the rest of the package. `MediaUrl` is the
absolute origin that media URLs sent to the cloud are rewritten onto, because the platform renders
them outside this site and a site-relative path would not resolve there. Left unset, it falls back
to the production base URL held in Umbraco settings content, and a site with neither raises an error
when it first has to build a media URL.
