# N3O.Umbraco.Email

Composes and sends email, on top of FluentEmail. `IEmailBuilder.Create<T>` starts a fluent builder
for a merge model, so the subject and body are templates merged against that model rather than
strings assembled by the caller.

Templates are Umbraco content. A template node carries the from name and address, an optional blind
copy address, the subject and the body, so an editor changes the wording of an email without a
deployment.

Sending is a command handled in the background, so a request does not wait on the provider. This
package does not talk to a provider itself: it defines the message and leaves the transport to a
sender package, and without one no email is sent.
