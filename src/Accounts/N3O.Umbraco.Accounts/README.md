# N3O.Umbraco.Accounts

Models the supporter account that giving, checkout and payments all share: an individual or an
organisation, with a name, address, email, telephone, consent choices and tax status. Each of those
is a request, response and validator triple, so the same shape is accepted over the API, validated
once and mapped consistently.

It also serves the data entry settings that tell a front end how to render an account form. Those
come from Umbraco content, not configuration, so an editor decides per field whether it is visible
and required, its label, help text and order, and chooses the name and address layouts. Address
settings carry a lookup API key for address completion, and the consent options are separate content
items so they can be added without a deployment.
