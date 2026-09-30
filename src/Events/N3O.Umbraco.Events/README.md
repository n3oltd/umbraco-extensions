# N3O.Umbraco.Events

Content types and querying for events: an events page, an events container, events and categories.
`IEventsFinder` takes criteria and returns events as whichever strongly-typed content model the
caller asks for, so a site can extend the event type and still use the finder.

Events are addressed by a URL that does not include the container node. A content finder resolves
those URLs and a URL provider generates them, so an event keeps one address wherever it is linked
from.
