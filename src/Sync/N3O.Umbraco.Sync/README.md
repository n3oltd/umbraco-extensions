# N3O.Umbraco.Sync

Enables uSync, so Umbraco schema and content can be serialised to disk and moved between
environments.

It adds one thing of its own: properties can be excluded from an import. An `ISyncFilter` claims a
content type alias and decides per property whether it should be imported, and the filters are
applied to content and media as each item is imported. That is how a property whose value belongs to
the environment — something set per site rather than authored — survives a sync from another one
instead of being overwritten.
