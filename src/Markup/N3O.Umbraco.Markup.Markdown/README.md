# N3O.Umbraco.Markup.Markdown

Renders Markdown to HTML with Markdig, implementing the engine that `N3O.Umbraco.Markup` defines. It
also replaces Umbraco's own Markdown value converter, so a Markdown property renders through this
pipeline wherever it appears rather than only where the tag helper is used.

On top of standard Markdown it adds helpers: a keyword in the source that takes arguments and
renders whatever the helper decides, which is how a site embeds something Markdown has no syntax
for. A helper declares its keywords and its argument count, and is discovered automatically, so
adding one is a matter of declaring the type. Pipe tables are enabled as well.

Helpers are applied in attribute order, which matters when two of them could claim the same keyword.
