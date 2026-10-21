# N3O.Umbraco.AI.MicrosoftFoundry

Adds Microsoft AI Foundry as a model provider for `N3O.Umbraco.AI`, so a connection can use the
models deployed in a Foundry resource.

It declares the `microsoft-foundry` connection, which `N3O.Umbraco.AI` keeps in step with
configuration when the site starts. The connection authenticates with the resource's endpoint and an
API key:

| Setting  | Configuration key                               | Required |
|----------|-------------------------------------------------|----------|
| API key  | `Umbraco:AI:Secrets:MicrosoftFoundryApiKey`     | Yes      |
| Endpoint | `Umbraco:AI:Variables:MicrosoftFoundryEndpoint` | Yes      |
