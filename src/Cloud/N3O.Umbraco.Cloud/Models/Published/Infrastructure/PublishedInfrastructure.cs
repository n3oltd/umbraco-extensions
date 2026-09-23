using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedInfrastructure : Value {
    public PublishedSearch Search { get; set; }

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Search;
    }
}
