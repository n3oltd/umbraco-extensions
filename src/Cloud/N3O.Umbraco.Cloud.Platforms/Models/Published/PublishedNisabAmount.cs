using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class PublishedNisabAmount : Value {
    public decimal Gold { get; set; }
    public decimal? GoldPricePerGram { get; set; }
    public decimal? GoldPricePerGramYesterday { get; set; }
    public decimal Silver { get; set; }
    public decimal? SilverPricePerGram { get; set; }
    public decimal? SilverPricePerGramYesterday { get; set; }

    protected override IEnumerable<object> GetAtomicValues() {
        yield return Gold;
        yield return GoldPricePerGram;
        yield return GoldPricePerGramYesterday;
        yield return Silver;
        yield return SilverPricePerGram;
        yield return SilverPricePerGramYesterday;
    }
}