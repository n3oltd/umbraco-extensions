# N3O.Umbraco.Video.YouTube

Reads YouTube channels and their videos, and renders a video into a page through a tag helper.

Videos and channel details are read by scraping rather than through the YouTube Data API, so there
is no API key to configure and no quota to exhaust. The corresponding cost is that it depends on
YouTube's own pages, which are not a contract; a change there breaks retrieval without any
configuration having changed.
