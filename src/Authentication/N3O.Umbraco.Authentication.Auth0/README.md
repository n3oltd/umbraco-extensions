# N3O.Umbraco.Authentication.Auth0

Signs Umbraco members and back office users in against Auth0, implementing the sign-in seam that
`N3O.Umbraco.Authentication` defines. Members and back office users are configured separately, each
with its own Auth0 application, so the two can point at different tenants or connections.

```json
{
  "Authentication": {
    "Members": {
      "Auth0": {
        "Login": {
          "Authority": "https://<tenant>.eu.auth0.com",
          "ClientId": "<client id>",
          "ClientSecret": "<from the secret store>",
          "Domain": "<tenant>.eu.auth0.com",
          "ConnectionName": "Username-Password-Authentication",
          "AutoCreateDirectoryUser": true,
          "Passwordless": false
        },
        "Management": { "ApiIdentifier": "https://<tenant>.eu.auth0.com/api/v2/" },
        "M2M": { "ApiIdentifier": "<api identifier>" }
      }
    },
    "BackOffice": { "Auth0": { } }
  }
}
```

`Login` is the interactive flow, `Management` reaches the Auth0 Management API to read and write the
user directory, and `M2M` obtains a token for calling our own APIs; each is a distinct Auth0
application and they are not interchangeable. `AutoCreateDirectoryUser` creates the Auth0 user on
save when one does not already exist, which is how someone added directly in Umbraco becomes able to
sign in; it is read from whichever of the two sections the account belongs to. With `Passwordless`
set, the user is created without a password, so sign-in has to go through a passwordless connection.
