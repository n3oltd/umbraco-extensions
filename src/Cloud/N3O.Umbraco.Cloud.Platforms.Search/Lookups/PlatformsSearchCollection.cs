using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Search.Lookups;

public class PlatformsSearchCollection : Lookup {
    public PlatformsSearchCollection(string id) : base(id) { }
}

public class PlatformsSearchCollections : StaticLookupsCollection<PlatformsSearchCollection> {
    public static readonly PlatformsSearchCollection Offerings = new(Clients.PlatformsSearchCollection.Platforms_offerings.ToEnumString());
}
