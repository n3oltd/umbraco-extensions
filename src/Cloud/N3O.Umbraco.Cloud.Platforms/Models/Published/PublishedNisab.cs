using NodaTime;
using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class PublishedNisab : Value {
    public Dictionary<string, PublishedNisabAmount> Amounts { get; set; }
    public decimal? GoldGrams { get; set; }
    public LocalDate? PricedOn { get; set; }
    public decimal? SilverGrams { get; set; }

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Amounts?.Keys;
        yield return Amounts?.Values;
        yield return GoldGrams;
        yield return PricedOn;
        yield return SilverGrams;
    }
}