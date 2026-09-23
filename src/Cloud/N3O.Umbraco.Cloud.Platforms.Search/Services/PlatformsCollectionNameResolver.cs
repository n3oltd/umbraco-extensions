using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;
using N3O.Umbraco.Cloud.Platforms.Search.Lookups;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Search;

public class PlatformsCollectionNameResolver : IPlatformsCollectionNameResolver {
    private readonly ICdnClient _cdnClient;

    public PlatformsCollectionNameResolver(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }

    public async Task<string> ResolveAsync(PlatformsSearchCollection collection,
                                           CancellationToken cancellationToken = default) {
        var infrastructure = await _cdnClient.DownloadSubscriptionContentAsync<PublishedInfrastructure>(SubscriptionFiles.Infrastructure,
                                                                                                         JsonSerializers.Simple,
                                                                                                         cancellationToken);
        var name = infrastructure?.Search?.Collections?.GetValueOrDefault(collection.Id);

        if (!name.HasValue()) {
            throw new Exception($"The published infrastructure file has no search collection {collection.Id.Quote()}");
        }

        return name;
    }
}
