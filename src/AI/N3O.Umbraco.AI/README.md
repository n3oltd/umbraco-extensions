# N3O.Umbraco.AI

Brings in Umbraco AI with its prompts and the back office copilot, so a site takes one dependency and
gets them at versions known to work together.

It contains no model provider: without a provider package nothing can be connected, so add one
alongside it (`N3O.Umbraco.AI.Anthropic`, `N3O.Umbraco.AI.MicrosoftFoundry`,
`N3O.Umbraco.AI.OpenAI`). Install it in the web project, which is where the back office assets have
to land.

Connections, profiles, prompts and agents are set up in the back office and stored in the site's
database, including each connection's API key.

`AIComposer` registers a chat middleware, `BudgetExceededChatMiddleware`, after Umbraco AI's
built-in middlewares. When a provider error means an AI gateway's spending limit is used up, the
middleware rewrites that error's message into one an editor can act on, on streaming and
non-streaming calls alike. Every other error passes through unchanged. Umbraco AI's tracking runs
inside the middleware and records the original error, and the rewritten exception keeps the original
as its inner exception.

Which errors mean that is up to the provider packages. Each contributes a class implementing
`IBudgetExceededMatcher`, which is registered automatically, and an error is rewritten when any of
them matches it. For a provider whose SDK raises a `ClientResultException`, `GetErrorType()` reads
the error type from the JSON body of the response that exception carries.
