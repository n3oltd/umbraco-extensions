# N3O.Umbraco.AI.Google

Adds Google as a model provider for `N3O.Umbraco.AI`, so a connection can use Gemini models.

It declares the `google` connection, which `N3O.Umbraco.AI` keeps in step with configuration
when the site starts:

| Setting | Configuration key                 | Required |
|---------|-----------------------------------|----------|
| API key | `Umbraco:AI:Secrets:GoogleApiKey` | Yes      |
