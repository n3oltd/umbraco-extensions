using N3O.Umbraco.Constants;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Search.Typesense.Models;
using N3O.Umbraco.Utilities;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class CollectionNameResolver : ICollectionNameResolver {
    public string Resolve(CollectionName name) {
        var environment = Site.Environment;
        var siteId = Site.Id;

        // Every site in an environment shares one Typesense cluster, so the environment and the site
        // are what keep one site's documents apart from another's.
        if (!environment.HasValue() || !siteId.HasValue()) {
            throw new Exception($"Cannot resolve the Typesense collection {name.Base.Quote()} because " +
                                $"{EnvironmentData.GetOurKey(EnvironmentVariables.Environment).Quote()} and " +
                                $"{EnvironmentData.GetOurKey(EnvironmentVariables.SiteId).Quote()} are both required");
        }

        return $"{environment}_{siteId}_{name.Base}";
    }
}
