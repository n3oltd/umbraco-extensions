# N3O.Umbraco.Cells.Data

Teaches content import and export about the cells property editor, by supplying the property
converter for it. The grid is carried as JSON in a single column.

Without it a cells property is skipped, because converters are matched on the property editor's
alias and no other package claims this one.
