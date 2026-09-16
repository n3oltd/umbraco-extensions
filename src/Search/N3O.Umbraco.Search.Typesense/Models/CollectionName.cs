using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;

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
        var collectionName = TypesenseCollections.Collections.SingleOrDefault(x => x.Key.EqualsInvariant(Base)).Value;

        if (collectionName.HasValue()) {
            return collectionName;
        }

        return Base;
    }
}