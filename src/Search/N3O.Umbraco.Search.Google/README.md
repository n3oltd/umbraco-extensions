# N3O.Umbraco.Search.Google

Searches the site through a Google Programmable Search Engine, and supplies the module that renders
results into the search block.

The API key and search engine identifier come from Umbraco settings content. Because the index is
Google's own, results only cover pages Google has crawled, so a page published moments ago will not
be found however the site is configured.
