# N3O.Umbraco.Uploader

A property editor for uploading a file or an image, with the constraints declared on the data type:
the allowed extensions, a maximum size in megabytes, and for images the minimum and maximum
dimensions.

Turning on images only does more than validate. It switches the editor to showing a preview and an
alt text field, so it changes the editing experience as well as what is accepted. Alt text can be
made mandatory separately.

The server enforces less than the data type declares. An upload is checked against the allowed
extensions, and, where images only is set, that the filename carries a known image extension; the
size and dimension limits are applied by the editor in the browser and are not rechecked on the
server. Both server-side checks are on the filename rather than the contents, so they classify a
file rather than verify it.
