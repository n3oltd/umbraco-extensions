# N3O.Umbraco.Cropper

A property editor that uploads an image and produces a fixed set of crops from it. Each crop is
defined on the data type with a label, alias, width, height and optional filters, so every property
using that data type yields the same named crops and a view asks for one by alias.

Crops are generated on the server when the content is saved and stored as media, rather than being
applied by URL parameters at request time, so what is served is a real file at the exact dimensions.
That also means changing a crop definition does not retrospectively change images already cropped.

Alt text is optional per data type. Where a cropper image is used as the Open Graph image, a handler
picks the appropriate crop, so social previews are not served the full-size original.
