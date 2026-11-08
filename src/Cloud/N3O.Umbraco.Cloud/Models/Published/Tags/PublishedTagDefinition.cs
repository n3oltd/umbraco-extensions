using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedTagDefinition : PublishedNamedLookup {
    public string Description { get; set; }
    public TagScope Scope { get; set; }
    public IEnumerable<string> Keys { get; set; }
    public bool AutoPopulated { get; set; }
}