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
using ZakatNisabGrams = N3O.Umbraco.Cloud.Platforms.Clients.ZakatNisabGrams;

namespace N3O.Umbraco.Cloud.Platforms.Templates;

public class NisabMergeModelProvider : MergeModelsProvider {
    private readonly IBaseCurrencyAccessor _baseCurrencyAccessor;
    private readonly IFormatter _formatter;
    private readonly INisab _nisab;

    public NisabMergeModelProvider(INisab nisab, IBaseCurrencyAccessor baseCurrencyAccessor, IFormatter formatter) {
        _baseCurrencyAccessor = baseCurrencyAccessor;
        _formatter = formatter;
        _nisab = nisab;
    }
    
    public override Task<bool> IsProviderForAsync(IPublishedContent content) => Task.FromResult(true);

    protected override async Task PopulateModelsAsync(IPublishedContent content,
                                                      Dictionary<string, object> mergeModels,
                                                      CancellationToken cancellationToken = default) {
        var baseCurrency = _baseCurrencyAccessor.GetBaseCurrency();
        
        var nisab = await _nisab.GetAsync(cancellationToken);
        var amount = nisab?.Amounts[baseCurrency.Code];

        var goldNisab = _formatter.Number.FormatMoney(amount?.Gold ?? 0m, baseCurrency);
        var silverNisab = _formatter.Number.FormatMoney(amount?.Silver ?? 0m, baseCurrency);

        var goldGrams = _formatter.Number.Format(ZakatNisabGrams.Gold);
        var goldPricePerGram = FormatPricePerGram(amount?.GoldPricePerGram, baseCurrency);
        var pricedOn = _formatter.DateTime.FormatDate(nisab?.PricedOn);
        var silverGrams = _formatter.Number.Format(ZakatNisabGrams.Silver);
        var silverPricePerGram = FormatPricePerGram(amount?.SilverPricePerGram, baseCurrency);

        mergeModels[PlatformsTemplateConstants.ModelKeys.Nisab] = new NisabMergeModel(goldNisab,
                                                                                      goldGrams,
                                                                                      goldPricePerGram,
                                                                                      pricedOn,
                                                                                      silverNisab,
                                                                                      silverGrams,
                                                                                      silverPricePerGram);
    }

    private string FormatPricePerGram(decimal? pricePerGram, Currency currency) {
        return pricePerGram.IfNotNull(x => _formatter.Number.FormatMoney(x, currency));
    }
}