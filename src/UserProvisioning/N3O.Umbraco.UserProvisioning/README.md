# N3O.Umbraco.UserProvisioning

Back office users are created, updated and deactivated by an identity provider rather than by hand.
The site exposes a SCIM 2.0 endpoint, the identity provider calls it whenever a directory group
changes, and Umbraco's user list follows. Administrators manage group membership in one place and
nothing else.

SCIM is a pull protocol. Nothing notifies the site. The identity provider polls its own directory
and calls the endpoint with what changed, so a group membership change reaches Umbraco on that
provider's cycle rather than immediately. Microsoft Entra runs an initial cycle and then incremental
cycles every 20 to 40 minutes.

## What the endpoint does

`/Users` and `/Groups` are served under a configurable base route, defaulting to `/umbraco/scim`.

The default sits under `/umbraco` deliberately. A staging site puts HTTP Basic authentication in
front of everything except the back office, and a provisioning service authenticates with a bearer
token and cannot satisfy a Basic challenge. Serving from `/umbraco/scim` places the endpoint on the
side of that gate the back office is already on, which is where the scheduled jobs dashboard sits
for the same reason. Moving the route outside `/umbraco` on a staging site makes it unreachable.

A person added to a mapped directory group becomes an approved Umbraco user in the user group that
group maps to. A person removed from every mapped group is disabled, because the provisioning
service sends `active` as false rather than a delete. A disabled user keeps their content history,
which is why `DELETE` also disables rather than removing the record.

User groups are never created, renamed or deleted through the endpoint. They belong to the site, and
the configuration decides which directory group governs which of them. A directory group the
configuration does not name is rejected.

Umbraco holds one name per user where SCIM sends `givenName` and `familyName` separately, so the two
are joined on write, and the single name is returned as `name.formatted` on read.

## Configuration

The endpoint is off until `Enabled` is true, so a site can carry the package and serve nothing. That
is the seam for enabling provisioning in one environment and not another.

```json
{
  "N3O": {
    "UserProvisioning": {
      "Enabled": true,
      "BaseRoute": "/umbraco/scim",
      "BearerToken": "<from the secret store>",
      "DefaultUserGroupAlias": "editor",
      "Licensee": "<licensee>",
      "LogRequests": false,
      "LicenseKey": "<licence key>",
      "UserGroups": {
        "CMS Editors": "editor",
        "CMS Administrators": "admin"
      }
    }
  }
}
```

`UserGroups` maps a directory group's display name to an Umbraco user group alias. The identity
provider matches groups on display name, so the key is what the directory calls the group and the
value is what Umbraco calls the user group. Two directory groups cannot map to one alias, and
startup fails if they do.

`DefaultUserGroupAlias` is the user group a person lands in when the provisioning service creates
them, before it sends their group membership, and it has to be one of the mapped user groups.
`LogRequests` logs each SCIM request and response, which is how a rejected call is diagnosed. It is
off by default because those bodies carry names and email addresses. `BearerToken` is the credential the identity provider
presents; it is compared over a fixed-time hash and belongs in a secret store rather than in
`appsettings.json`. A missing token, licence or group map fails startup rather than serving an
endpoint that would accept anything or provision nobody.

## Connecting Microsoft Entra

Register the site as an enterprise application, choosing to integrate an application that is not in
the gallery, then under Provisioning:

1. Set the tenant URL to the base route, for example `https://example.org/umbraco/scim`.
2. Set the secret token to the configured `BearerToken`.
3. Test the connection, then assign the directory groups named in `UserGroups`.

Two behaviours are worth knowing before the first cycle. The provisioning service caches the `id`
this endpoint returns for each person and never searches for them again, so restoring the site's
database from elsewhere leaves those cached identifiers pointing at nothing and the job has to be
restarted. And a job whose calls keep failing is quarantined: its cycles slow to once a day and it
is disabled after four weeks, so a broken endpoint stops provisioning quietly and needs alerting on.

## Licence

SCIM support is provided by
[Rsk.AspNetCore.Scim](https://www.identityserver.com/products/scim-for-aspnet), which is licensed
separately from this package. Set `Licensee` and `LicenseKey` from that licence.
