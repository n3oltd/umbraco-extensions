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
      "AdministratorGroups": "CMS Administrators",
      "EditorGroups": "CMS Editors;CMS Contributors",
      "LogRequests": false
    }
  }
}
```

A directory group named in neither setting is refused, and a group named in both fails startup.
`BearerToken` belongs in a secret store. `LogRequests` is off by default because request bodies
carry names and email addresses.
