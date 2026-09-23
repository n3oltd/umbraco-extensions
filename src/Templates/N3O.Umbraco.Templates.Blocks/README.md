# N3O.Umbraco.Templates.Blocks

Runs the template merger over the HTML that blocks render, by supplying a post processor to the
blocks pipeline.

That makes a placeholder written in a block's content behave the same as one written anywhere else,
which it otherwise would not, because block HTML is produced after the point at which content is
normally merged. Merging happens after rendering, so a placeholder produced by a block's view is
merged as well as one typed by an editor.
