using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedTagValuesEntry {
    public string Key { get; set; }
    public string Name { get; set; }
    public IEnumerable<string> Values { get; set; }
}
