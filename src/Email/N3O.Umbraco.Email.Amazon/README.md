# N3O.Umbraco.Email.Amazon

Sends email through Amazon Simple Email Service. It supplies the sender that `N3O.Umbraco.Email`
uses, handing SES the raw MIME message, which is what allows attachments and a blind copy address to
survive.

The access key, secret key and region code come from Umbraco settings content. They are read once
when the sender is constructed, so a change to them takes effect on the next application start
rather than on the next send.
