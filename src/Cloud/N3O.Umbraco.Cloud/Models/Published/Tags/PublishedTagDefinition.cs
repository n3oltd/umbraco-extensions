using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedTagDefinition : PublishedNamedLookup {
    public TagScope Scope { get; set; }
    public IEnumerable<string> Keys { get; set; }
}