# N3O.Umbraco.SerpEditor.Data

Teaches content import and export about the SERP editor property, by supplying the property
converter for it, so the title and description travel as columns of their own.

Without it a SERP editor property is skipped, because converters are matched on the property
editor's alias and no other package claims this one.
