# N3O.Umbraco.Newsletters

Exposes a subscribe endpoint that takes a contact and hands it to whichever newsletter provider is
registered, so a signup form on a site does not depend on which provider is behind it.

`INewslettersClient` is the whole abstraction and this package does not implement it; a provider
package does. A refused subscription is answered as unprocessable with the provider's error details
rather than as a failure, so the caller can tell a rejected address apart from an outage.
