using N3O.Umbraco.Constants;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Utilities;
using System;

namespace N3O.Umbraco.Search.Typesense;

public class CollectionNameResolver : ICollectionNameResolver {
    public string Resolve(string name) {
        var environment = Site.Environment;
        var siteId = Site.Id;

        // Every site in an environment shares one Typesense cluster, so the environment and site
        // are what keep their documents apart. Composing a name without them would silently point
        // a site at another site's collection
        if (!environment.HasValue() || !siteId.HasValue()) {
            throw new Exception($"Cannot resolve the Typesense collection {name.Quote()} because " +
                                $"{EnvironmentData.GetOurKey(EnvironmentVariables.Environment).Quote()} and " +
                                $"{EnvironmentData.GetOurKey(EnvironmentVariables.SiteId).Quote()} are both required");
        }

        return $"{environment}_{siteId}_{name}";
    }
}
