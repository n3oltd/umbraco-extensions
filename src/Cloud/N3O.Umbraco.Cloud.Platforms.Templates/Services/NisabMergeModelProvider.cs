using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Cloud.Platforms.Templates.Models;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Templates;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Cloud.Platforms.Templates;

public class NisabMergeModelProvider : MergeModelsProvider {
    private readonly IBaseCurrencyAccessor _baseCurrencyAccessor;
    private readonly ICdnClient _cdnClient;
    private readonly IFormatter _formatter;
    private readonly INisab _nisab;

    public NisabMergeModelProvider(INisab nisab,
                                   IBaseCurrencyAccessor baseCurrencyAccessor,
                                   ICdnClient cdnClient,
                                   IFormatter formatter) {
        _baseCurrencyAccessor = baseCurrencyAccessor;
        _cdnClient = cdnClient;
        _formatter = formatter;
        _nisab = nisab;
    }
    
    public override Task<bool> IsProviderForAsync(IPublishedContent content) => Task.FromResult(true);

    protected override async Task PopulateModelsAsync(IPublishedContent content,
                                                      Dictionary<string, object> mergeModels,
                                                      CancellationToken cancellationToken = default) {
        var baseCurrency = _baseCurrencyAccessor.GetBaseCurrency();
        
        var goldNisabAmount = await _nisab.GetGoldNisabAsync(baseCurrency, cancellationToken);
        var goldNisab = _formatter.Number.FormatMoney(goldNisabAmount);
        var silverNisabAmount = await _nisab.GetSilverNisabAsync(baseCurrency, cancellationToken);
        var silverNisab = _formatter.Number.FormatMoney(silverNisabAmount);

        var nisab = await _cdnClient.DownloadSubscriptionContentAsync<PublishedNisab>(SubscriptionFiles.Nisab,
                                                                                      JsonSerializers.JsonProvider,
                                                                                      cancellationToken);
        var amount = nisab?.Amounts[baseCurrency.Code];

        var goldGrams = _formatter.Number.Format(nisab?.GoldGrams);
        var goldPriceChange = FormatChange(amount?.GoldPricePerGram, amount?.GoldPricePerGramYesterday);
        var goldPricePerGram = FormatPricePerGram(amount?.GoldPricePerGram, baseCurrency);
        var pricedOn = _formatter.DateTime.FormatDate(nisab?.PricedOn);
        var silverGrams = _formatter.Number.Format(nisab?.SilverGrams);
        var silverPriceChange = FormatChange(amount?.SilverPricePerGram, amount?.SilverPricePerGramYesterday);
        var silverPricePerGram = FormatPricePerGram(amount?.SilverPricePerGram, baseCurrency);

        mergeModels[PlatformsTemplateConstants.ModelKeys.Nisab] = new NisabMergeModel(goldNisab,
                                                                                      goldGrams,
                                                                                      goldPriceChange,
                                                                                      goldPricePerGram,
                                                                                      pricedOn,
                                                                                      silverNisab,
                                                                                      silverGrams,
                                                                                      silverPriceChange,
                                                                                      silverPricePerGram);
    }

    private string FormatChange(decimal? pricePerGram, decimal? pricePerGramYesterday) {
        if (pricePerGram == null || pricePerGramYesterday == null) {
            return null;
        }

        var change = (pricePerGram.Value - pricePerGramYesterday.Value) / pricePerGramYesterday.Value;

        return _formatter.Number.FormatPercentage(change);
    }

    private string FormatPricePerGram(decimal? pricePerGram, Currency currency) {
        return pricePerGram.IfNotNull(x => _formatter.Number.FormatMoney(x, currency));
    }
}