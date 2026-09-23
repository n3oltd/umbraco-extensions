# N3O.Umbraco.Search.Typesense

Indexes content into Typesense and searches it, so results come from an index the site controls
rather than from an external crawler.

A document type declares its own schema with attributes: `[Collection]` names the collection and its
version, `[Field]` maps a property to a Typesense field and states whether it is indexed, facetable,
sortable and so on, and `[Index]` adds further indexed fields derived from one property. The schema
is therefore a consequence of the type, and changing a field means changing the type rather than
editing anything in Typesense.

Collection names are not used as written. Configuration maps a declared collection name onto the
name actually used on the server, which is what allows several sites to share one Typesense cluster,
and what allows a collection to be rebuilt under a new name and switched over. A name with no entry
in that map is used unchanged, so a missing entry quietly shares another site's collection.

Content is indexed when it is published and removed when it is unpublished or deleted, and commands
exist to index one item or everything of a type, which is how a site is first populated or repaired.

```json
{
  "Typesense": {
    "ApiKey": "<from the secret store>",
    "SearchApiKey": "<a search-only key>",
    "Node": "<the cluster host>",
    "Port": 443,
    "Collections": {
      "pages": "pages-thissite"
    }
  }
}
```

The client is only constructed when the API key, node and port are all present; with any of them
missing it resolves as null rather than failing at startup, so a site without Typesense starts
normally and fails at the point something searches. `SearchApiKey` is the key handed to the browser
and should be search-only — `ApiKey` can write to the index. The connection is always made over
HTTPS.
