# N3O.Umbraco.Authentication

Defines the sign-in seam that an identity provider package implements, and exposes the two member
endpoints that do not depend on which provider is in use: sign out, and redirect to the provider's
password reset page.

`ISignInManager` is the whole abstraction, and this package does not implement it. Without a package
that does, the endpoints will not resolve. Configuration lives under an `Authentication` section
with `BackOffice` and `Members` subsections, and the extension methods here read those sections so a
provider package does not restate the paths.
