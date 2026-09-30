# N3O.Umbraco.Video

Renders an embedded video from a URL with the `<n3o-video video-url="...">` tag helper. Add
`@addTagHelper *, N3O.Umbraco.Video` to `Views/_ViewImports.cshtml` to use it.

This package recognises no URLs on its own. Each video platform is a separate package, and a site
embeds videos only from the platforms whose packages it references:

- `N3O.Umbraco.Video.Facebook`
- `N3O.Umbraco.Video.Instagram`
- `N3O.Umbraco.Video.TikTok`
- `N3O.Umbraco.Video.YouTube`

A URL that no referenced platform recognises renders nothing. Attributes on the tag are forwarded to
the emitted `iframe`.

A platform is a class implementing `IVideoPlatform`, which is registered automatically.
