# N3O.Umbraco.TaxRelief

Defines what a tax relief scheme is, so the giving packages can ask whether a gift qualifies and
what it is worth without knowing which country's scheme is in force. A scheme decides eligibility
from the supporter's country of residence and whether they are an organisation, and calculates the
relief a gift attracts on a given date.

A site runs one scheme, chosen in Umbraco settings content. This package supplies none, so a country
package is required; with none installed there is no scheme to choose.

Tax status is deliberately three-valued — payer, non-payer and not specified — because a supporter
who has not answered is not the same as one who has said no, and only the first of those can be
revisited.
