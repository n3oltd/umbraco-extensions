# N3O.Umbraco.Payments

The payment abstraction the provider packages implement. A payment method is a lookup that declares
the two things a provider may offer — taking a payment now, and storing a credential for taking
payments later — by naming the CLR type it uses for each, so a provider that only stores credentials
is one whose payment type is null and is never offered for a one-off gift.

A payment method also names the Umbraco content type that holds its settings, which is how a site
enables a provider: create its settings node. It may restrict itself by country and currency, and
the payment methods query filters on that, so a supporter is only offered what can actually take
their money.

A payment object records its own history. Rather than a status field being assigned, each outcome —
paid, declined, an error, three-D Secure required, three-D Secure completed — is a method on the
object, so the sequence a payment went through is recoverable afterwards and a provider cannot leave
it in a state the model does not describe. `IPaymentsScope.DoAsync` runs an operation against the
flow and its payment object and returns the result, so error handling and persistence are not
repeated in every provider.
