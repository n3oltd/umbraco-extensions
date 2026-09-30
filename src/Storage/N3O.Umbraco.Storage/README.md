# N3O.Umbraco.Storage

File storage for the things that are not Umbraco media: import and export workbooks, generated
files, anything a package needs to keep. `IVolume` hands out a folder by path and the folder reads,
writes and lists blobs within it, so a caller never composes a physical path.

There are two entry points because some work happens before the container is built: `IVolume` is
resolved from the container, while `IStartupStorage` takes configuration directly for use during
startup.

Storage is on local disk unless a provider package replaces it, which makes a single server work
with no configuration and is exactly what does not survive more than one instance.
