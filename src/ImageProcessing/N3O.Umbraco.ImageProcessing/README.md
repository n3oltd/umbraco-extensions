# N3O.Umbraco.ImageProcessing

Generates images on the server with ImageSharp. `IImageBuilder` starts from a source file or a blank
canvas of a given size and colour, and the fluent builder applies operations — resize, constrain to
a size, crop to an aspect ratio and auto-orient from EXIF.

`IImagePublisher` writes the result to media and returns its published URL, keyed by a cache key the
caller composes. The same key returns the existing image rather than regenerating it, so the key has
to include everything that affects the output; leave a variable out and callers will be served
another one's image. `forcePublish` regenerates regardless, which is the way to replace an image
whose key cannot change.
