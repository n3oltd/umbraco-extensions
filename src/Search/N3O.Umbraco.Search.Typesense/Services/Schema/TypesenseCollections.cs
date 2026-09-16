using System.Collections.Generic;

namespace N3O.Umbraco.Search.Typesense;

public class TypesenseCollections {
    public static IReadOnlyDictionary<string, string> Collections { get; private set; } = new Dictionary<string, string>();
    
    // TypesenseSearchComposer passes the Typesense section's Collections, and Get returns null for a
    // section that is absent, which every site without Typesense configured has. Collections is read
    // unguarded when a name is resolved, so keep it empty rather than null.
    internal static void Initialize(Dictionary<string, string> collections) {
        Collections = collections ?? new Dictionary<string, string>();
    }
}