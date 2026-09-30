# N3O.Umbraco.Giving.Analytics

Reports giving to Google Analytics as ecommerce. It maps a completed checkout onto a purchase event,
with one item per allocation, and supplies the tag helper that emits it.

It bridges two packages that do not otherwise know about each other: the analytics data layer and
the checkout. Place the tag helper on the checkout complete page, since the event describes a
checkout that has finished.
