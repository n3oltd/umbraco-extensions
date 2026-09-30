# N3O.Umbraco.Newsletters.Mailchimp

Subscribes contacts to a Mailchimp audience, implementing the client that `N3O.Umbraco.Newsletters`
defines.

The API key and audience identifier come from Umbraco settings content. Both are read once when the
client is constructed, so a change takes effect on the next application start.
