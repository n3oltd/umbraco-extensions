# N3O.Umbraco.Email.Smtp

Sends email over SMTP, supplying the sender that `N3O.Umbraco.Email` uses.

The host, port and credentials are taken from Umbraco settings content when a settings node exists,
and otherwise from Umbraco's own `Global:Smtp` configuration, so a site can keep its SMTP details
wherever it already holds them. The two are not merged: as soon as the settings node exists it is
used whole, and a partly filled node silently overrides a complete configuration section. The
connection always uses SSL.
