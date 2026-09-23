# N3O.Umbraco.Payments.Bambora

Takes card payments and stores cards through Bambora, supporting both one-off payments and stored
credentials for regular giving.

Three-D Secure is handled as part of the payment's own history rather than as a separate flow: a
payment that needs a challenge records that it does, and the completion is a further step on the
same object, so an abandoned challenge is distinguishable from a decline.

The merchant identifier and the two passcodes come from Umbraco settings content. Bambora uses
separate passcodes for taking payments and for the profiles that hold stored cards, so a site that
does both needs both, and a missing profile passcode fails only when a card is first stored.
