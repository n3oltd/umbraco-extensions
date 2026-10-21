# N3O.Umbraco.AI

Brings in Umbraco AI with its prompts and the back office copilot, so a site takes one dependency
and gets them at versions known to work together.

It has no model provider of its own: without a provider package nothing can be connected, so add
one alongside it (`N3O.Umbraco.AI.Anthropic`, `N3O.Umbraco.AI.Google`,
`N3O.Umbraco.AI.MicrosoftFoundry`, `N3O.Umbraco.AI.OpenAI`). Install it in the web project, which is
where the back office assets have to land.

## Connections

Connections come from configuration, not the back office. Each provider package declares one
connection: its alias, its Umbraco AI provider, and the configuration keys its settings read, which
the provider package's README lists. API keys sit under `Umbraco:AI:Secrets` and other settings
under `Umbraco:AI:Variables`.

Each time the site starts at runtime level Run, for each installed provider package:

- When the provider's required keys are present, the connection with its alias is created if
  missing, and its settings are set to references to the keys (`$Umbraco:AI:Secrets:...`),
  replacing whatever was set on it in the back office. Umbraco AI resolves a reference from
  configuration each time the connection is used, so no key is stored in the database.
- When a required key is absent, the connection with its alias is deleted. Umbraco AI refuses to
  delete a connection that a profile uses; that connection stays and a warning names the profiles.
- A connection with the alias but a different provider is left unchanged, with a warning.

Profiles are never deleted, and connections under any other alias are never touched. Profiles,
prompts and agents are set up in the back office and stored in the site's database.

## Error messages

When a provider refuses a request for a reason that retrying will not fix, the editor sees a message
they can act on instead of the provider's own:

| Condition                                 | Message                                                                                       |
|-------------------------------------------|-----------------------------------------------------------------------------------------------|
| The AI account has run out of credit      | The AI account for this site has run out of credit. Contact IT to top it up.                  |
| A gateway's monthly allowance is used up  | This site has used this month's AI allowance. It starts again on the 1st, or IT can add more. |

`AIErrorMessageChatMiddleware` wraps every chat client after Umbraco AI's own middlewares, for
streaming and non-streaming calls alike. It asks each registered `IAIErrorMatcher` whether an error
means one of these conditions, and rethrows a matched error with that condition's message, keeping
its category, its provider code and the original error as its inner exception, so Umbraco AI's
tracking records the original. Every other error passes through unchanged.

Each provider package registers a matcher for its own provider's errors, and
`N3O.Umbraco.AI.LiteLLM` registers one for a LiteLLM gateway in front of any provider.
