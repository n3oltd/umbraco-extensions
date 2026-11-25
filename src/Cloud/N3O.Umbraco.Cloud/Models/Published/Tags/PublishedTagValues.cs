using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Models;

public class PublishedTagValues {
    public IEnumerable<PublishedTagValuesEntry> Entries { get; set; }
}
