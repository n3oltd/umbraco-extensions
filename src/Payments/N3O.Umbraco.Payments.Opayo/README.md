# N3O.Umbraco.Payments.Opayo

Takes card payments and stores cards through Opayo, including Apple Pay and Google Pay, and supports
both one-off payments and stored credentials.

A card payment begins by obtaining a merchant session key, which the browser uses to tokenise the
card so the details never reach the site. Apple Pay needs a further session obtained from Opayo, so
that is a separate step. Three-D Secure, including the fallback path, is recorded on the payment
object as it happens.

The vendor name, integration key and password, and the base URL come from Umbraco settings content.
The base URL is what selects Opayo's live or test system, so it has to be changed in step with the
credentials.
