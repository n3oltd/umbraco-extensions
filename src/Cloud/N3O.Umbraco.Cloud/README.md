# N3O.Umbraco.Cloud

Connects an Umbraco site to the N3O cloud APIs. It resolves which subscription and data region the
site belongs to, builds the API, CDN and webhook URLs for that region, and creates the HTTP clients
the feature packages call through.

The subscription identifier and data region are read from environment data, not from Umbraco
content, so a site that is missing them cannot reach the cloud at all; both are also added to the
log context so entries can be attributed to a subscription. Reference data that the cloud owns —
countries, currencies, fund dimensions, giving schedules, sponsorship schemes and the rest — is
exposed as lookups, so a site consumes it through `ILookups` like any other lookup rather than
calling the API directly.

Files the cloud has published for the subscription are served back through the site under a path
derived from the subscription code, which lets a browser fetch them from the site's own origin. That
route is a passthrough to the CDN, so nothing is stored locally.

```json
{
  "CdnCache": {
    "MaxAge": "00:05:00",
    "NotFoundRetryInterval": "00:01:00"
  }
}
```

`MaxAge` is how long a fetched file is reused before the CDN is asked again.
`NotFoundRetryInterval` is the separate, shorter wait applied after a miss, so a file that has not
been published yet is picked up soon after it appears rather than after a full `MaxAge`.
