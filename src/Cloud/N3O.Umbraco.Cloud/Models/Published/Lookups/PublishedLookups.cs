using Newtonsoft.Json;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedLookups : Value {
    [JsonProperty("subscriptions.countries")]
    public PublishedLookupCollection<PublishedCountry> Countries { get; set; }

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Countries;
    }
}