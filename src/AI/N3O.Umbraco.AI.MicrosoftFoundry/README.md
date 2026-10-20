# N3O.Umbraco.AI.MicrosoftFoundry

Adds Microsoft AI Foundry as a model provider for `N3O.Umbraco.AI`, so a back office connection can use
the models deployed in a Foundry resource.

The connection takes the Foundry endpoint and either an API key or Entra ID credentials, entered in
the back office.

`MicrosoftFoundryBudgetExceededMatcher` tells the budget middleware in `N3O.Umbraco.AI` which errors
mean an AI gateway's spending limit is used up: a provider error whose response body, read from the
SDK's exception, has the error type `budget_exceeded`.
