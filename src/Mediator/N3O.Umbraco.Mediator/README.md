# N3O.Umbraco.Mediator

Wraps MediatR in a request abstraction where the request type, its model and its response are three
separate type arguments, so a handler is found from the request type alone and the model stays a
plain object that can be bound from a controller.

Requests derive from `Request<TModel, TResponse>` and are registered automatically, as are handlers,
across every assembly `OurAssemblies` recognises. A request with no meaningful response uses `None`,
and `IMediator.SendAsync` has an overload for that case so a caller does not have to name the
response type.
