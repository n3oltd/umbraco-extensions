# N3O.Umbraco.Giving.Cart

Holds the gifts a visitor has chosen before they check out. The cart is an entity with explicit
operations — add, remove, bulk add, bulk remove, clear and reset — rather than a mutable list, so
every change to its contents goes through one place and can be validated.

The cart is identified by a cookie, so it survives a visitor leaving and returning without them
signing in. A cart block renders the current contents into a page.

A cart is revalidated against the current offer, and against the visitor's current currency, rather
than trusted because it was valid when it was filled. That is what catches a gift added before an
editor changed or withdrew the option behind it, or before the visitor switched currency. Validation
answers only valid or not, so a cart that fails is rejected whole rather than partly repaired.

Upsell offers are added and removed separately from ordinary gifts, because an upsell is attached to
the gift that prompted it.
