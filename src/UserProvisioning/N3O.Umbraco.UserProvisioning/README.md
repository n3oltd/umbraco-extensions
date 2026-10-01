# N3O.Umbraco.UserProvisioning

Creates, updates and deactivates Umbraco back office users from an identity provider over SCIM 2.0,
so administrators manage directory group membership and nothing else. Removal deactivates rather
than deletes, keeping the user's content history.

`AdministratorGroups` and `EditorGroups` name the directory groups that fill each role, separated by
semicolons. The Umbraco user group each role provisions into is fixed by the package. The endpoint
is off until `Enabled` is true, and only users in one of the named groups are visible to it.

```json
{
  "N3O": {
    "UserProvisioning": {
      "Enabled": true,
      "BaseRoute": "/umbraco/scim",
      "BearerToken": "<from the secret store>",
      "GovernedDomains": "example.org;example.com",
      "AdministratorGroups": "CMS Administrators",
      "EditorGroups": "CMS Editors;CMS Contributors",
      "LogRequests": false
    }
  }
}
```

`GovernedDomains` lists the email domains the identity provider owns. A user whose address is outside
them is invisible to the endpoint whatever user group holds them, so accounts created by hand for
people the directory does not know are never read, changed or disabled. A create, or a change of a
user's address, that would take them outside these domains is refused, and so is one that names an
address another user already holds. A disable that such a request carries still takes effect.

A directory group named in neither setting is refused, and a group named in both fails startup.
`BearerToken` belongs in a secret store. `LogRequests` is off by default because request bodies
carry names and email addresses.

## Upgrading from Umbraco 13

Umbraco 14 gives every back office user a new key, and a user's key is their SCIM `id`. On the first
start with the endpoint enabled, a migration moves the stored external IDs, name parts and directory
group memberships onto the new keys, so memberships the directory claimed are kept. The identity
provider still holds the old IDs, which now answer 404, so clear its provisioning state and restart
provisioning once the site is upgraded; it then matches each user again by `userName` and picks up
the new IDs.
