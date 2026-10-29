using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedCulture : Value {
    public string Language { get; set; }

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Language;
    }
}
