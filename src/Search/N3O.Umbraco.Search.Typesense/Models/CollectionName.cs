using N3O.Umbraco.Constants;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Generic;

namespace N3O.Umbraco.Search.Typesense.Models;

public class CollectionName : Value {
    public CollectionName(string @base) {
        Base = @base;
    }
    
    public string Base { get; }


    
    protected override IEnumerable<object> GetAtomicValues() {
        yield return Base;
    }
    
    public string Resolve() {
        var environment = Site.Environment;
        var siteId = Site.Id;

        // Every site in an environment shares one Typesense cluster, so the environment and the site
        // are what keep one site's documents apart from another's.
        if (!environment.HasValue() || !siteId.HasValue()) {
            throw new Exception($"Cannot resolve the Typesense collection {Base.Quote()} because " +
                                $"{EnvironmentData.GetOurKey(EnvironmentVariables.Environment).Quote()} and " +
                                $"{EnvironmentData.GetOurKey(EnvironmentVariables.SiteId).Quote()} are both required");
        }

        return $"{environment}_{siteId}_{Base}";
    }
}