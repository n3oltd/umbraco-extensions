# N3O.Umbraco.Data

Imports and exports Umbraco content as spreadsheets. An editor starts an export or an import from a
content app on the node itself, the work runs as a background job, and the result is a workbook in
Excel or CSV form.

A column is produced per property by an `IPropertyConverter`, which is chosen by the property
editor's alias, so a property editor this package has never heard of is exported and imported
correctly as soon as a package ships a converter for it. Filters let a package exclude content or
properties from either direction, and content matchers decide whether an imported row updates an
existing node or creates one.

Imported values are parsed rather than assigned. Each cell goes through a parser for its data type
and a failure is recorded against the row in an error log instead of aborting the import, so one bad
cell does not lose the rest of the file. Date patterns and the decimal separator are part of the
parse settings, because the same digits mean different dates and different numbers depending on
them, and a wrong setting produces a file that imports without error and with wrong values.

It also carries the table and workbook builders the rest of the estate uses to produce spreadsheets,
where attributes on a row class declare the column title, order, formatting and alignment.
