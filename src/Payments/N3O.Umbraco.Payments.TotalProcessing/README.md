# N3O.Umbraco.Payments.TotalProcessing

Takes card payments and stores cards through Total Processing, supporting both one-off payments and
stored credentials.

Both begin by preparing a checkout with Total Processing and end when the result is reported back,
and each half is recorded on the payment object. Requests are sent form encoded rather than as JSON,
which the API requires.

The entity identifier, access token and base URL come from Umbraco settings content. The base URL
selects the live or test system and the other two are issued against one of them, so a mismatched
set fails when a payment is attempted rather than at startup.
