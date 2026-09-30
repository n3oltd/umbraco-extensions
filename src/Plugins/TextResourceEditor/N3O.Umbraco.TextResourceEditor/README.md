# N3O.Umbraco.TextResourceEditor

A property editor for editing a set of named text resources on a single property, exposing the value
as a collection of text resources that the localisation machinery reads.

It is how a site's own wording — the strings that are not page content but still need to be editable
— is held in Umbraco rather than in resource files, which means changing a string is an editor's job
and takes effect without a deployment.
