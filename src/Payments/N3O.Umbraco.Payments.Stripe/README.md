# N3O.Umbraco.Payments.Stripe

Takes card payments and stores cards through Stripe, using payment intents for one-off gifts and
setup intents for stored credentials.

Both follow the same two steps — the intent is created here and confirmed after the browser has
handled whatever Stripe asked the supporter for — and each step is recorded on the payment object,
so a payment that is awaiting confirmation is not mistaken for one that failed. Strong customer
authentication is Stripe's to handle within that exchange, so there is no separate three-D Secure
path here as there is for other providers.

The publishable and secret keys come from Umbraco settings content. A Stripe customer is created
from the supporter's billing details so that a stored card belongs to a customer rather than
standing alone; it is created each time rather than looked up, so a supporter who gives twice is two
customers in Stripe.
