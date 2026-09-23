# N3O.Umbraco.Validation

Validation for API requests, built on FluentValidation. Every `IValidator<T>` in our assemblies is
registered automatically, and a MediatR pipeline behaviour runs the validator for a request model
before the handler sees it, so a handler can assume a valid model.

Failures leave the handler as a `ValidationException`, which middleware converts into an RFC 7807
problem details response with one entry per failed property. Property names in messages come from
the `[Name]` attribute rather than the C# identifier.

It also carries the validators shared across packages: country codes against ISO 3166, telephone
numbers against libphonenumber, monetary amounts, ranges, and a profanity guard for free text.
