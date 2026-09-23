# N3O.Umbraco.Payments.PayPal

Takes payments through PayPal, as one-off transactions and as subscriptions for regular giving.

A PayPal subscription needs a product and a plan to exist before it can be created, and a plan is
specific to an amount, currency and frequency. Rather than requiring those to be set up by hand,
this package finds or creates the plan for the gift being made, so a new combination works the first
time a supporter chooses it.

The client identifier and base URL come from Umbraco settings content; the base URL is what selects
PayPal's live or sandbox system.
