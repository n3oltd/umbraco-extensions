# N3O.Umbraco.AI.Anthropic

Adds Anthropic as a model provider for `N3O.Umbraco.AI`, so a back office connection can use Claude
models.

The API key is entered on the connection in the back office.

`AnthropicAIComposer` registers a chat middleware, `BudgetExceededChatMiddleware`, after Umbraco
AI's built-in middlewares. An AI gateway that enforces a spending limit reports an exhausted limit
as a provider error with the code `budget_exceeded`, and the middleware rewrites that error's
message into one an editor can act on, on streaming and non-streaming calls alike. Every other error
passes through unchanged. Umbraco AI's tracking runs inside the middleware and records the original
error, and the rewritten exception keeps the original as its inner exception.
