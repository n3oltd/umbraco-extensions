using N3O.Umbraco.Context;
using N3O.Umbraco.Metadata;
using System.Collections.Generic;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsLanguageMetadataProvider : IMetadataProvider {
    private readonly ICultureAccessor _cultureAccessor;
    private readonly IPlatformsLanguageResolver _platformsLanguageResolver;

    public PlatformsLanguageMetadataProvider(ICultureAccessor cultureAccessor,
                                             IPlatformsLanguageResolver platformsLanguageResolver) {
        _cultureAccessor = cultureAccessor;
        _platformsLanguageResolver = platformsLanguageResolver;
    }

    public Task<bool> IsProviderForAsync(IPublishedContent _) {
        return Task.FromResult(true);
    }

    public async Task<IEnumerable<MetadataEntry>> GetEntriesAsync(IPublishedContent _) {
        var entries = new List<MetadataEntry>();
        var language = await _platformsLanguageResolver.ResolveAsync(_cultureAccessor.GetCulture());

        if (language != null) {
            entries.Add(new MetadataEntry("n3o-language", language.Id));
        }

        return entries;
    }
}
