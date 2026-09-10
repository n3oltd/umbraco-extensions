using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Lookups;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.GeoIP;

public class GeoIPDefaultCurrencyProvider : LookupsDefaultCurrencyProvider {
    private readonly IIPGeoLocationProvider _ipGeoLocationProvider;

    public GeoIPDefaultCurrencyProvider(ILookups lookups, IIPGeoLocationProvider ipGeoLocationProvider)
        : base(lookups) {
        _ipGeoLocationProvider = ipGeoLocationProvider;
    }

    public override async Task<Currency> GetDefaultCurrencyAsync(CancellationToken cancellationToken = default) {
        var geoLookupResult = await _ipGeoLocationProvider.GeoLocateAsync(cancellationToken);

        var currency = default(Currency);
        
        if (geoLookupResult.Success) {
            currency = geoLookupResult.Country.GetCurrency(AllCurrencies);
        }

        if (currency == null) {
            currency = await base.GetDefaultCurrencyAsync(cancellationToken);
        }

        return currency;
    }
}