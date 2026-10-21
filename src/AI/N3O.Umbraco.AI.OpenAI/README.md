# N3O.Umbraco.AI.OpenAI

Adds OpenAI as a model provider for `N3O.Umbraco.AI`, so a connection can use GPT models.

It declares the `openai` connection, which `N3O.Umbraco.AI` keeps in step with configuration
when the site starts:

| Setting  | Configuration key                     | Required                           |
|----------|---------------------------------------|------------------------------------|
| API key  | `Umbraco:AI:Secrets:OpenAIApiKey`     | Yes                                |
| Endpoint | `Umbraco:AI:Variables:OpenAIEndpoint` | No; the provider default applies   |
