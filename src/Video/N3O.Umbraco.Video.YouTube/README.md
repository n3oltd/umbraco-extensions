# N3O.Umbraco.Video.YouTube

Lets `<n3o-video>` (see `N3O.Umbraco.Video`) embed YouTube videos from `youtube.com`, `youtu.be` and
`youtube-nocookie.com` URLs, keeping the `youtube-nocookie.com` host when the URL uses it.

The package also provides the older `<n3o-youtube-video video-url="...">` tag helper, which renders
YouTube URLs the same way and nothing for any other URL.

Separately from embedding, `IYouTube` reads channels and their videos. Those details are read by
scraping rather than through the YouTube Data API, so there is no API key to configure and no quota
to exhaust. The corresponding cost is that it depends on YouTube's own pages, which are not a
contract; a change there breaks retrieval without any configuration having changed.
