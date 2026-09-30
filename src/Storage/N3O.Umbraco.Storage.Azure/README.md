# N3O.Umbraco.Storage.Azure

Moves media, the ImageSharp cache, data protection keys and general file storage into Azure Blob
Storage, so instances share them instead of each holding its own copy on disk.

It configures itself from `Umbraco:Storage:AzureBlob:Media`, and does nothing at all if that section
is absent, leaving the site on disk storage. Within it, `ServiceUrl` and `ConnectionString` are
mutually exclusive and choose how the account is reached: `ServiceUrl` uses
`DefaultAzureCredential`, so no storage secret is held in configuration, and `ConnectionString` uses
the key in the string. Setting both does not combine them — the credential path wins.

`SingleContainer` decides where general file storage goes. With it set, files share the media
container under a storage prefix; without it they go to a separate container in the same account.
Changing it on an existing site does not move anything, so files written under the previous setting
stop being found.

Data protection keys are persisted to blob storage in both modes. That is what keeps cookies and
antiforgery tokens valid across instances and restarts; without it every restart signs everyone out.
