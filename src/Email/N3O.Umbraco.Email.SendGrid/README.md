# N3O.Umbraco.Email.SendGrid

Sends email through SendGrid, supplying the sender that `N3O.Umbraco.Email` uses.

The API key and the sandbox flag come from Umbraco settings content and are read once when the
sender is constructed. With sandbox mode on, SendGrid validates a message and reports success
without delivering it, so an environment left in sandbox looks healthy and sends nothing.
