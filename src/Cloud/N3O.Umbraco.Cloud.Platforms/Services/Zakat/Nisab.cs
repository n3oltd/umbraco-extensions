using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Models;
using N3O.Umbraco.Exceptions;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms;

public class Nisab : INisab {
    private readonly ICdnClient _cdnClient;

    public Nisab(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }

    public async Task<PublishedNisab> GetAsync(CancellationToken cancellationToken = default) {
        return await _cdnClient.DownloadSubscriptionContentAsync<PublishedNisab>(SubscriptionFiles.Nisab,
                                                                                 JsonSerializers.JsonProvider,
                                                                                 cancellationToken);
    }

    public async Task<Money> GetGoldNisabAsync(Currency currency, CancellationToken cancellationToken = default) {
        return await GetNisabAsync(currency, NisabTypes.Gold, cancellationToken);
    }
    
    public async Task<Money> GetNisabAsync(Currency currency,
                                           NisabType nisabType,
                                           CancellationToken cancellationToken = default) {
        var nisab = await GetAsync(cancellationToken);

        if (nisab == null) {
            return currency.Zero();
        }
        
        if (!nisab.Amounts.ContainsKey(currency.Code)) {
            throw new Exception($"No nisab value found for currency {currency.Code}");
        }

        decimal nisabAmount;

        if (nisabType == NisabTypes.Gold) {
            nisabAmount = nisab.Amounts[currency.Code].Gold;
        } else if (nisabType == NisabTypes.Silver) {
            nisabAmount = nisab.Amounts[currency.Code].Silver;
        } else {
            throw UnrecognisedValueException.For(nisabType);
        }

        return new Money(nisabAmount, currency);
    }
    
    public async Task<Money> GetSilverNisabAsync(Currency currency, CancellationToken cancellationToken = default) {
        return await GetNisabAsync(currency, NisabTypes.Silver, cancellationToken);
    }
}