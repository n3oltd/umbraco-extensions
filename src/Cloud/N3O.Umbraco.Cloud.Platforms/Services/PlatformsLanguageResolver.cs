using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using System.Globalization;
using System.Linq;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsLanguageResolver : IPlatformsLanguageResolver {
    private readonly ILookups _lookups;

    public PlatformsLanguageResolver(ILookups lookups) {
        _lookups = lookups;
    }

    public PlatformsLanguage Resolve(string cultureCode) {
        var languages = _lookups.GetAll<PlatformsLanguage>();

        for (var culture = CultureInfo.GetCultureInfo(cultureCode);
             !culture.Equals(CultureInfo.InvariantCulture);
             culture = culture.Parent) {
            var language = languages.SingleOrDefault(x => x.Id.EqualsInvariant(culture.Name));

            if (language != null) {
                return language;
            }
        }

        return null;
    }
}
