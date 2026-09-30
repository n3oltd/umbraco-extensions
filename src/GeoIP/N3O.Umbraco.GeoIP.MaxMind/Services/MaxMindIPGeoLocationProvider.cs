using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.GeoIP.Models;
using N3O.Umbraco.Lookups;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.GeoIP.MaxMind;

public class MaxMindIPGeoLocationProvider : IIPGeoLocationProvider {
    private const int ResultsCacheSizeLimit = 10_000;

    private static readonly TimeSpan ResultsCacheLifetime = TimeSpan.FromHours(12);
    private static readonly MemoryCache ResultsCache = CreateResultsCache();

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

        var result = await ResultsCache.GetOrCreateAsync(ipAddress, async c => {
            c.AbsoluteExpirationRelativeToNow = ResultsCacheLifetime;
            c.Size = 1;

            return await LookupAsync(ipAddress);
        });

        // A failure says nothing about the address, only that this lookup did not answer, so it is not kept:
        // caching it would report no location for this address until the entry expired.
        if (!result.Success) {
            ResultsCache.Remove(ipAddress);
        }

        return result;
    }

    private async Task<GeoLookupResult> LookupAsync(IPAddress ipAddress) {
        try {
            var cityResponse = await _webServiceClient.CityAsync(ipAddress);

            var country = _lookups.GetAll<Country>().FindByCode(cityResponse.Country.IsoCode);

            return GeoLookupResult.ForSuccess(country,
                                              cityResponse.City?.Name,
                                              cityResponse.MostSpecificSubdivision?.Name);
        } catch (GeoIP2Exception) {
            // The service answered but could not locate the address, or rejected the request.
        } catch (HttpException) {
            // The service could not be reached. The result is not cached, so the next lookup retries.
        }

        return GeoLookupResult.ForFailure();
    }

    private static MemoryCache CreateResultsCache() {
        var options = new MemoryCacheOptions();

        // Without a size limit every unique visitor address accumulates for the process lifetime.
        options.SizeLimit = ResultsCacheSizeLimit;

        return new MemoryCache(options);
    }
}
