# N3O.Umbraco.Maps.Google

Lets a Google Maps property be declared in code rather than configured by hand in the back office.
It supplies the data type designer and the property type builder for the `Our.Umbraco.GMaps` editor,
so a content type adds a map property with its API key, starting location and zoom set from code.

The API key is part of the data type's configuration, not the property's, so every property using
that data type shares it.
