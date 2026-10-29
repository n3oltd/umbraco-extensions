using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

[Order(int.MaxValue)]
public class ApiPlatformsLanguages : ApiLookupsCollection<PlatformsLanguage> {
    private readonly ICdnClient _cdnClient;

    public ApiPlatformsLanguages(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }

    protected override async Task<IReadOnlyList<PlatformsLanguage>> FetchAsync(CancellationToken cancellationToken) {
        var publishedLocalization = await _cdnClient.DownloadSubscriptionContentAsync<PublishedLocalization>(SubscriptionFiles.Localization,
                                                                                                             JsonSerializers.JsonProvider,
                                                                                                             cancellationToken);

        var languages = new List<PlatformsLanguage>();

        foreach (var publishedCulture in publishedLocalization.OrEmpty(x => x.Cultures)) {
            var language = new PlatformsLanguage(publishedCulture.Language);

            languages.Add(language);
        }

        return languages;
    }

    protected override TimeSpan ReloadInterval => TimeSpan.FromMinutes(1);
}
