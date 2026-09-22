using N3O.Umbraco.Extensions;
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
}