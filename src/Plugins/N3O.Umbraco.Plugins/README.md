# N3O.Umbraco.Plugins

The base a back office property editor's server side is built on. `PluginController` gives an editor
an authorised, JSON API endpoint under the back office route with the response types already
declared, and the validating variant adds request validation.

It also carries what the upload-based editors share: multipart request handling that streams a file
rather than buffering the whole request, the uploaded file and image models, and image metadata
extraction. The other plugin packages build their editors on this, so the upload handling is written
once.
