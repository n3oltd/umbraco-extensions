# N3O.Umbraco.Newsletters.SendGrid

Subscribes contacts to a SendGrid marketing list, implementing the client that
`N3O.Umbraco.Newsletters` defines. It talks to the marketing API directly rather than through the
SendGrid library's mail helpers, because contacts, lists and custom field definitions are not part
of that surface.

The API key and list identifier come from Umbraco settings content. Custom fields are matched to
SendGrid's field definitions by name, so a field the list does not define is not sent.
