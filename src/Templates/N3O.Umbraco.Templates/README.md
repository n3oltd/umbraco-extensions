# N3O.Umbraco.Templates

Merges values into text, so an editor can write a placeholder in content and have it filled at
render time. `IMerger` merges for a piece of content, and tag helpers merge a block of content or a
partial from a view.

What is available to merge comes from `IMergeModelsProvider`. A provider says whether it applies to
the content being rendered and contributes named models, and providers are discovered automatically,
so a package makes its own data mergeable without this package knowing about it.

Values are formatted by an `ITemplateFormatter` chosen for the type, which is why a date or a money
amount merges in the site's own format rather than as the CLR default.

No template syntax is included. An engine package supplies `ITemplateEngine`, and without one
nothing merges.
