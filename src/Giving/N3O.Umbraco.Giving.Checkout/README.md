# N3O.Umbraco.Giving.Checkout

Takes a cart through to a completed gift. The checkout is an entity that records the supporter's
account details, consent and tax status, the regular giving options where they apply, and the
payment flow, and it is the thing a payment is taken against.

Checkout is staged, and the stages are a lookup rather than a hard-coded sequence. Each stage says
whether it is required for a given checkout and whether it is complete, which page renders it and
whether it can be revisited, so a checkout that needs no regular giving stage skips it and the
supporter is sent to the right page without the controller enumerating cases. The account stage is
always required and can be revisited; the payment stages cannot.

What happens after a payment is driven by a change feed over the checkout entity rather than by the
controller. When a stage becomes complete the feed sends that stage's receipt, whose template is
Umbraco content, dispatches the webhook for it, and clears the corresponding part of the cart. A
single donation and a regular gift are separate stages, so a checkout containing both produces two
receipts and two webhooks.

A dev tools endpoint resends a checkout's webhook, which is how a gift recorded here but not
received downstream is repaired without touching the data.
