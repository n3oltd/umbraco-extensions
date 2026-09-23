using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedSearch : Value {
    public Dictionary<string, string> Collections { get; set; }

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Collections?.Keys;
        yield return Collections?.Values;
    }
}
