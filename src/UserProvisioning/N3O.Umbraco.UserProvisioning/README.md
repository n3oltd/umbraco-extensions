# N3O.Umbraco.UserProvisioning

Creates, updates and deactivates Umbraco back office users from an identity provider over SCIM 2.0,
so administrators manage directory group membership and nothing else. Removal deactivates rather
than deletes, keeping the user's content history.

Configuration maps a directory group's display name to an Umbraco user group alias. The endpoint is
off until `Enabled` is true, and only users in a mapped group are visible to it.

```json
{
  "N3O": {
    "UserProvisioning": {
      "Enabled": true,
      "BaseRoute": "/umbraco/scim",
      "BearerToken": "<from the secret store>",
      "DefaultUserGroupAlias": "editor",
      "LogRequests": false,
      "UserGroups": {
        "CMS Editors": "editor",
        "CMS Administrators": "admin"
      }
    }
  }
}
```

`DefaultUserGroupAlias` is where a newly provisioned user lands before their group membership
arrives, and has to be one of the mapped aliases. `BearerToken` belongs in a secret store.
`LogRequests` is off by default because request bodies carry names and email addresses.
