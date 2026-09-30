# N3O.Umbraco.Giving

The public giving API. It serves the fund structure, the donation form for a given identifier, the
lookups a giving front end needs, and a pricing endpoint that prices a set of criteria rather than
making the caller reproduce the pricing rules.

Setting the currency is a request rather than a query string, because the chosen currency is held
for the visitor and affects every price served afterwards.

It also receives the donation items webhook, so the items a site can be given towards follow what
the backend publishes rather than being maintained in Umbraco.
