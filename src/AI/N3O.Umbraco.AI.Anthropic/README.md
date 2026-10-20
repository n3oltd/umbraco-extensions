# N3O.Umbraco.AI.Anthropic

Adds Anthropic as a model provider for `N3O.Umbraco.AI`, so a back office connection can use Claude
models.

The API key is entered on the connection in the back office.

`AnthropicBudgetExceededMatcher` tells the budget middleware in `N3O.Umbraco.AI` which errors mean
an AI gateway's spending limit is used up: a provider error with the code `budget_exceeded`.
