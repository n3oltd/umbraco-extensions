# N3O.Umbraco.Giving.Allocations

Models what a gift is allocated to. An allocation is one of four types — fund, sponsorship, feedback
or Qurbani — and each carries the fund dimensions, scheme and options that type needs, so the rest
of the giving packages handle a mixed basket without branching on type.

Editors define the giving offer in Umbraco: donation forms, donation options per allocation type,
prices with their handles and pricing rules, and upsell offers. A price is worked out from those
rules by the price calculator rather than being read off the option, and the amount a supporter has
entered is checked against it, so a client cannot submit an amount that the offer does not allow.

Fund dimensions are a fixed set of four, exposed as lookups whose values come from the backend, so
their names and values are the subscription's rather than the site's.

An allocation can be extended by another package through the request binder and validator seams,
which is how a package adds its own fields to an allocation without this one knowing about them.
