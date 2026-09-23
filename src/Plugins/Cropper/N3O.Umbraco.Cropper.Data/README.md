# N3O.Umbraco.Cropper.Data

Teaches content import and export about the cropper property editor, by supplying the property
converter for it.

Without it a cropper property is skipped, because converters are matched on the property editor's
alias and no other package claims this one.
