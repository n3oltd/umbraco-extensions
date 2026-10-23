# N3O.Umbraco.Cropper

A property editor that uploads an image and produces a fixed set of crops from it. Each crop is
defined on the data type with a label, alias, width, height and optional filters, so every property
using that data type yields the same named crops and a view asks for one by alias.

Crops are generated on the server when the content is saved and stored as media, rather than being
applied by URL parameters at request time, so what is served is a real file at the exact dimensions.
That also means changing a crop definition does not retrospectively change images already cropped.

A data type can name a crop suggester, a registered `ICropSuggester` chosen by alias. After an
upload the editor places each crop box where the suggester proposes, and data import uses the
suggestion in place of the default crop; with no suggestion the default applies, and whatever is in
the crop box when the content is saved is what gets stored. Suggestions are in the image's stored
pixel frame, before any EXIF orientation is applied.

Alt text is optional per data type. Where a cropper image is used as the Open Graph image, a handler
picks the appropriate crop, so social previews are not served the full-size original.
