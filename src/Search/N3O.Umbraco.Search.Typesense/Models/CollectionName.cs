using N3O.Umbraco.Extensions;
using System;
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
        return TryResolve() ?? Base;
    }

    // Resolve falls back to Base, which is right for a collection this application owns: the startup task
    // creates it under the same resolved name, so an unmapped name is merely unscoped rather than wrong.
    // A collection another application owns cannot be created here, so the fallback would search a
    // collection that does not exist and return nothing rather than fail. Require the mapping instead.
    public string ResolveRequired() {
        var collectionName = TryResolve();

        if (!collectionName.HasValue()) {
            throw new Exception($"Typesense collection {Base.Quote()} is not mapped in the Typesense:Collections " +
                                $"configuration. Collections are named per subscription, so {Base.Quote()} is a " +
                                $"key into that configuration rather than the name of a collection.");
        }

        return collectionName;
    }

    private string TryResolve() {
        return TypesenseCollections.Collections.SingleOrDefault(x => x.Key.EqualsInvariant(Base)).Value;
    }
}