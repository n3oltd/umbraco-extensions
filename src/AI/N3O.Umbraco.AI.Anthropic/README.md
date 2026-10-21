# N3O.Umbraco.AI.Anthropic

Adds Anthropic as a model provider for `N3O.Umbraco.AI`, so a connection can use Claude models.

It declares the `anthropic` connection, which `N3O.Umbraco.AI` keeps in step with configuration when
the site starts:

| Setting  | Configuration key                        | Required                           |
|----------|------------------------------------------|------------------------------------|
| API key  | `Umbraco:AI:Secrets:AnthropicApiKey`     | Yes                                |
| Endpoint | `Umbraco:AI:Variables:AnthropicEndpoint` | No; the provider default applies   |
