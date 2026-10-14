# N3O.Umbraco.Email.Smtp

Sends email over SMTP, supplying the sender that `N3O.Umbraco.Email` uses.

The host, port and credentials are taken from Umbraco settings content when a settings node exists,
and otherwise from Umbraco's own `Global:Smtp` configuration, so a site can keep its SMTP details
wherever it already holds them. The two are not merged: as soon as the settings node exists it is
used whole, and a partly filled node silently overrides a complete configuration section.

Port 465 is encrypted from the moment of connecting, and any other port must upgrade with STARTTLS,
so email is never sent unencrypted. The sender logs in only when both a username and a password are
set; with either missing it connects without logging in.
