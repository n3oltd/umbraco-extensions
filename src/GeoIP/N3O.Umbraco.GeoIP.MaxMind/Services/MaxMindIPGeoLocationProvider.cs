using MaxMind.GeoIP2;
using Microsoft.Extensions.Caching.Memory;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.GeoIP.Models;
using N3O.Umbraco.Lookups;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.GeoIP.MaxMind;

public class MaxMindIPGeoLocationProvider : IIPGeoLocationProvider {
    // SizeLimit caps cache growth — without it, every unique visitor IP accumulates forever
    private static readonly MemoryCache ResultsCache = new(new MemoryCacheOptions { SizeLimit = 10_000 });

    private readonly ILookups _lookups;
    private readonly IRemoteIpAddressAccessor _remoteIpAddressAccessor;
    private readonly WebServiceClient _webServiceClient;

    public MaxMindIPGeoLocationProvider(ILookups lookups,
                                        IRemoteIpAddressAccessor remoteIpAddressAccessor,
                                        WebServiceClient webServiceClient) {
        _lookups = lookups;
        _remoteIpAddressAccessor = remoteIpAddressAccessor;
        _webServiceClient = webServiceClient;
    }

    public async Task<GeoLookupResult> GeoLocateAsync(CancellationToken cancellationToken = default) {
        var ipAddress = _remoteIpAddressAccessor.GetRemoteIpAddress();

        if (ipAddress == null) {
            return GeoLookupResult.ForFailure();
        }

        return await ResultsCache.GetOrCreateAsync(ipAddress, async c => {
            c.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
            c.Size = 1;

            try {
                var cityResponse = await _webServiceClient.CityAsync(ipAddress);

                var country = _lookups.GetAll<Country>().FindByCode(cityResponse.Country.IsoCode);

                return GeoLookupResult.ForSuccess(country,
                                                  cityResponse.City?.Name,
                                                  cityResponse.MostSpecificSubdivision?.Name);
            } catch { }

            return GeoLookupResult.ForFailure();
        });
    }
}
