# N3O.Umbraco.Uploader.Data

Teaches content import and export about the uploader property editor, by supplying the property
converter for it.

Without it an uploader property is skipped, because converters are matched on the property editor's
alias and no other package claims this one.
