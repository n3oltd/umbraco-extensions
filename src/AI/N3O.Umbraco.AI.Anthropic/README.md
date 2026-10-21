# N3O.Umbraco.AI.Anthropic

Adds Anthropic as a model provider for `N3O.Umbraco.AI`, so a connection can use Claude models.

It declares the `anthropic` connection, which `N3O.Umbraco.AI` keeps in step with configuration when
the site starts:

| Setting  | Configuration key                        | Required                           |
|----------|------------------------------------------|------------------------------------|
| API key  | `Umbraco:AI:Secrets:AnthropicApiKey`     | Yes                                |
| Endpoint | `Umbraco:AI:Variables:AnthropicEndpoint` | No; the provider default applies   |

`AnthropicOutOfCreditMatcher` tells `N3O.Umbraco.AI` that the account has reached its credit or
spending limit when Anthropic refuses a request because its credit balance is too low, because it
has reached a spend limit set in the Claude Console (HTTP 400, a message beginning "You have
reached your specified"), or because it has reached its usage tier's monthly spend cap (HTTP 429,
error code `enforced_spend_limit_reached`).
