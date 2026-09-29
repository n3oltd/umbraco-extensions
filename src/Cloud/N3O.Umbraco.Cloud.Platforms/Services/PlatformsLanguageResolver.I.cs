using N3O.Umbraco.Cloud.Platforms.Lookups;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

public interface IPlatformsLanguageResolver {
    Task<PlatformsLanguage> ResolveAsync(string cultureCode, CancellationToken cancellationToken = default);
}
