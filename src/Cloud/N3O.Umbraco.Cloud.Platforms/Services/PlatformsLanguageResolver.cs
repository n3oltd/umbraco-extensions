using N3O.Umbraco.Cloud.Platforms.Lookups;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsLanguageResolver : IPlatformsLanguageResolver {
    private readonly ILookups _lookups;

    public PlatformsLanguageResolver(ILookups lookups) {
        _lookups = lookups;
    }

    public async Task<PlatformsLanguage> ResolveAsync(string cultureCode, CancellationToken cancellationToken = default) {
        var languages = await _lookups.GetAllAsync<PlatformsLanguage>(cancellationToken);

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
