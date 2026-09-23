# N3O.Umbraco.SerpEditor

A property editor for the title and description a page should present to search engines, shown in
the back office as a preview of the search result so an editor sees the effect of the length limits
rather than being told about them.

The values are surfaced to the page through a page module and two tag helpers, so a layout emits the
page title and meta description from the property without each view reading it.

The editor reads a title suffix from content — the part appended to every page's title, typically
the organisation's name — so the preview shows the whole title as a search engine would, and the
suffix is changed in one place rather than on every page.
