# N3O.Umbraco.Cloud.Platforms.Templates

Exposes platform content to the template merge engine, so an email or page template can refer to the
current campaign, its offerings, the signed-in user and the current Nisab values by name.

Each is a merge model provider discovered by `N3O.Umbraco.Templates`; adding this package is what
makes those names resolve, and without it a template referring to them merges them as empty.
