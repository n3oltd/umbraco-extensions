using N3O.Umbraco.Cloud.Platforms.Lookups;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IPlatformsLanguageResolver {
    PlatformsLanguage Resolve(string cultureCode);
}
