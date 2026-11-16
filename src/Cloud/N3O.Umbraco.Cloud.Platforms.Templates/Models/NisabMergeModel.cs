namespace N3O.Umbraco.Cloud.Platforms.Templates.Models;

public class NisabMergeModel {
    public NisabMergeModel(string gold,
                           string goldGrams,
                           string goldPriceChangeSinceYesterday,
                           string goldPricePerGram,
                           string pricedOn,
                           string silver,
                           string silverGrams,
                           string silverPriceChangeSinceYesterday,
                           string silverPricePerGram) {
        Gold = gold;
        GoldGrams = goldGrams;
        GoldPriceChangeSinceYesterday = goldPriceChangeSinceYesterday;
        GoldPricePerGram = goldPricePerGram;
        PricedOn = pricedOn;
        Silver = silver;
        SilverGrams = silverGrams;
        SilverPriceChangeSinceYesterday = silverPriceChangeSinceYesterday;
        SilverPricePerGram = silverPricePerGram;
    }

    public string Gold { get; }
    public string GoldGrams { get; }
    public string GoldPriceChangeSinceYesterday { get; }
    public string GoldPricePerGram { get; }
    public string PricedOn { get; }
    public string Silver { get; }
    public string SilverGrams { get; }
    public string SilverPriceChangeSinceYesterday { get; }
    public string SilverPricePerGram { get; }
}