# N3O.Umbraco.AI.LiteLLM

Tells editors when a LiteLLM gateway has used up the site's AI allowance. Install it alongside
`N3O.Umbraco.AI` and a provider package whose connection points at the gateway.

`LiteLLMAllowanceUsedUpMatcher` tells `N3O.Umbraco.AI` that the allowance is used up when the
gateway refuses a request with the error type `budget_exceeded`. It reads that type from the
provider code that Umbraco AI records for Anthropic, and from the response body of the SDK exception
for OpenAI and Microsoft AI Foundry. Google is not covered, because its connection cannot point at a
gateway.
