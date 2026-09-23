# N3O.Umbraco.Clients

Generated HTTP clients for the APIs that the other packages in this repository expose, for use by
applications that call an Umbraco site from outside it. It has no dependency on Umbraco itself.

`AddUmbracoClients` registers every client against a base URL resolved per request, so one call
wires up the accounts, data, giving, cart, checkout, newsletters, payments, cropper and uploader
clients together. The optional `httpClientFactory` argument replaces the default
`IHttpClientFactory` client where a caller needs its own handler chain or authentication.
