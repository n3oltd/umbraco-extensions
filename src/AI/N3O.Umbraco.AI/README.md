# N3O.Umbraco.AI

Brings in Umbraco AI with its prompts and the back office copilot, so a site takes one dependency and
gets them at versions known to work together.

It contains no code of its own, and no model provider: without a provider package nothing can be
connected, so add one alongside it (`N3O.Umbraco.AI.Anthropic`, `N3O.Umbraco.AI.MicrosoftFoundry`,
`N3O.Umbraco.AI.OpenAI`). Install it in the web project, which is where the back office assets have to
land.

Connections, profiles, prompts and agents are set up in the back office and stored in the site's
database, including each connection's API key.
