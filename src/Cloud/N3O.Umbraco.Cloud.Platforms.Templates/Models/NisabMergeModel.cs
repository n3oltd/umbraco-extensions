namespace N3O.Umbraco.Cloud.Platforms.Templates.Models;

public class NisabMergeModel {
    public NisabMergeModel(string gold,
                           string goldGrams,
                           string goldPricePerGram,
                           string pricedOn,
                           string silver,
                           string silverGrams,
                           string silverPricePerGram) {
        Gold = gold;
        GoldGrams = goldGrams;
        GoldPricePerGram = goldPricePerGram;
        PricedOn = pricedOn;
        Silver = silver;
        SilverGrams = silverGrams;
        SilverPricePerGram = silverPricePerGram;
    }

    public string Gold { get; }
    public string GoldGrams { get; }
    public string GoldPricePerGram { get; }
    public string PricedOn { get; }
    public string Silver { get; }
    public string SilverGrams { get; }
    public string SilverPricePerGram { get; }
}