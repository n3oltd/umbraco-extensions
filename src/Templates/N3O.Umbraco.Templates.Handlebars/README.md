# N3O.Umbraco.Templates.Handlebars

Provides Handlebars as the template engine, implementing what `N3O.Umbraco.Templates` defines.

On top of standard Handlebars it registers helpers and block helpers, both discovered automatically
so a package adds its own by declaring a type. The block helpers are the comparisons a template
needs and Handlebars does not have — equality, ordering, string length, membership of a set, and
first, last, odd and even within an iteration — and the helpers cover formatting a number, a date,
a boolean, an ordinal and money, and resolving an absolute URL.

Every value a template writes out goes through the formatters the merger uses, not only the ones
passed to a formatting helper, so a date or a money amount printed directly from the model is
formatted the same way as one merged outside a template.
