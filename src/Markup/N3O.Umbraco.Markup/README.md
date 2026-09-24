# N3O.Umbraco.Markup

Defines the seam for rendering a markup language to HTML. `IMarkupEngine` renders and validates a
string, and a tag helper renders a property's content through whichever engine is registered.

This package supplies no engine, so an engine package is required; without one the tag helper will
not resolve. Empty content suppresses the element entirely rather than producing an empty tag.
