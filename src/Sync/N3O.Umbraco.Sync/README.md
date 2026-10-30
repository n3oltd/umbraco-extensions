# N3O.Umbraco.Sync

Enables uSync, so Umbraco schema and content can be serialised to disk and moved between
environments.

Properties can be excluded from an import. An `ISyncFilter` claims a content type alias and decides
per property whether it should be imported, and the filters are applied to content and media as each
item is imported. That is how a property whose value belongs to the environment — something set per
site rather than authored — survives a sync from another one instead of being overwritten.

A uSync Publisher push sends shared (culture-invariant) values even when only some languages are
selected, so a change to a shared property reaches the target whichever languages are pushed. When a
push is limited to some languages, an empty shared value never overwrites a value the target already
has. Clearing a shared value on the target takes a push of all languages.
