# N3O.Umbraco.Payments.GoCardless

Sets up Direct Debit mandates through GoCardless. It stores a credential only, so it is offered for
regular giving and not for a single gift, and it offers itself only for the United Kingdom and for
sterling.

The mandate is set up by a redirect flow: the supporter is sent to GoCardless, and the flow is
completed when they return, which is the point at which the mandate exists. Both halves are recorded
on the credential, so a supporter who never came back is distinguishable from one whose mandate
failed.

The access token and the environment come from Umbraco settings content. The environment selects
between GoCardless's live and sandbox systems, and a token issued for one is not valid against the
other.
