using N3O.Umbraco.Json;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.Search.Typesense;

public class TypesenseJsonProvider : JsonProvider<TypesenseJsonContractResolver>, ITypesenseJsonProvider {
    public TypesenseJsonProvider(IEnumerable<JsonConverter> jsonConverters) : base(jsonConverters) { }
}