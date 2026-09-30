# N3O.Umbraco.Sync.Extensions

Moves data and content between environments, alongside the schema and content that uSync handles. A
producer exposes a set of objects on one site and a consumer takes them on another, and the pair is
registered together through `DataSync` with the type they exchange and a shared secret.

Registration is where the pairing is checked. The producer and consumer are constrained to the same
exchanged type by their generic arguments, and both must carry `[DataSyncProvider]` with the same
identifier or registration throws, so a mismatched pair fails at startup rather than at transfer.

A registration is either on demand, triggered through the API, or scheduled at an interval, which
must be at least five minutes.

Content syncs separately. A published content model marked `[SyncOnPublish]` with a server alias is
queued for transfer to that server whenever it is published, which turns a manual push into
something that follows editing.
