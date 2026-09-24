# N3O.Umbraco.Payments.DirectDebitUK

Collects UK Direct Debit instructions. It stores a credential only — there is no one-off payment
through this method — so it is offered for regular giving and not for a single gift.

It offers itself only for the United Kingdom and for sterling, so it disappears from the available
methods for any other country or currency rather than failing later.

Sort code and account number are validated against a bank validation service before the instruction
is stored, through either Fetchify or Loqate. Which one is used follows from the Umbraco settings
content: the first validator whose API key is set is chosen, so set one key, not both.
