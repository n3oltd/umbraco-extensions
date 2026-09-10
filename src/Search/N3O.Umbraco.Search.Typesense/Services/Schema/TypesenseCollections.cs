using System.Collections.Generic;

namespace N3O.Umbraco.Search.Typesense;

public class TypesenseCollections {
    public static IReadOnlyDictionary<string, string> Collections { get; private set; } = new Dictionary<string, string>();
    
    internal static void Initialize(Dictionary<string, string> collections) {
        Collections = collections;
    }
}