using System.Collections.Generic;

namespace N3O.Umbraco.Search.Typesense;

public class TypesenseCollections {
    public static IReadOnlyDictionary<string, string> Collections { get; private set; } = new Dictionary<string, string>();
    
    internal static void Initialize(Dictionary<string, string> collections) {
        // The composer reads this from a Typesense configuration section that no site is obliged to
        // define, and binding an absent section yields null. Assigning that null replaced the empty
        // dictionary above, so every later lookup threw instead of falling back to the base name.
        Collections = collections ?? new Dictionary<string, string>();
    }
}