# N3O.Umbraco.AI.OpenAI

Adds OpenAI as a model provider for `N3O.Umbraco.AI`, so a back office connection can use GPT models.

The API key is entered on the connection in the back office.

`OpenAIBudgetExceededMatcher` tells the budget middleware in `N3O.Umbraco.AI` which errors mean an
AI gateway's spending limit is used up: a provider error whose response body, read from the SDK's
exception, has the error type `budget_exceeded`.
