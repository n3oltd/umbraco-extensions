using NodaTime;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class PublishedNisab : Value {
    public Dictionary<string, PublishedNisabAmount> Amounts { get; set; }
    public LocalDate? PricedOn { get; set; }

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Amounts?.Keys;
        yield return Amounts?.Values;
        yield return PricedOn;
    }
}