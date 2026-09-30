# N3O.Umbraco.KeyVault

Adds Azure Key Vault as a configuration source, so secrets are read from the vault instead of being
held in `appsettings.json`. It is enabled by setting `AzureKeyVaultUrl`; with that unset or not a
valid URL the vault is skipped and the application starts on its other configuration sources alone.

Authentication uses `DefaultAzureCredential`, so the same build works from a managed identity in
Azure and from a developer's signed-in credentials locally.

Secret names are translated rather than used verbatim, because Key Vault allows only letters,
digits and hyphens. A double hyphen becomes the configuration separator and a single hyphen becomes
an underscore, so `Foo--Bar-Baz` in the vault is `Foo:Bar_Baz` in configuration. That second rule is
the one to watch: a secret name meant to contain a literal hyphen cannot be expressed.
