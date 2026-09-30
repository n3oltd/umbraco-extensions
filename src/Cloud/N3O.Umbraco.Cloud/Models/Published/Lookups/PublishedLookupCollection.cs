using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedLookupCollection<T> : Value where T : PublishedLookup {
    [JsonProperty("items")]
    public IEnumerable<T> Items { get; set; }

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Items;
    }
}