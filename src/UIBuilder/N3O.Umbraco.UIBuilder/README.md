# N3O.Umbraco.UIBuilder

Enables Konstrukt, which builds back office editing interfaces over data that is not Umbraco
content, and adds the base a site configures it from.

Deriving from `KonstruktConfigurator` lets several packages each contribute their own collections;
`GetContentSection` returns the shared content section builder rather than a new one, so a second
configurator adds to the section the first created instead of replacing it.
