using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.GeoIP.Models;
using N3O.Umbraco.Lookups;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.GeoIP.MaxMind;

[Order(1)]
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

        if (ResultsCache.TryGetValue<GeoLookupResult>(ipAddress, out var cachedResult)) {
            return cachedResult;
        }

        try {
            var result = await LookupAsync(ipAddress);

            CacheResult(ipAddress, result);

            return result;
        } catch (AddressNotFoundException) {
            var result = GeoLookupResult.ForFailure();

            CacheResult(ipAddress, result);

            return result;
        } catch (Exception ex) when (ex is GeoIP2Exception or
                                           HttpException or
                                           HttpRequestException or
                                           TaskCanceledException) {
            return GeoLookupResult.ForFailure();
        }
    }

    private async Task<GeoLookupResult> LookupAsync(IPAddress ipAddress) {
        var cityResponse = await _webServiceClient.CityAsync(ipAddress);

        var country = _lookups.GetAll<Country>().FindByCode(cityResponse.Country.IsoCode);

        if (country == null) {
            return GeoLookupResult.ForFailure();
        }

        return GeoLookupResult.ForSuccess(country,
                                          cityResponse.City?.Name,
                                          cityResponse.MostSpecificSubdivision?.Name);
    }

    private static void CacheResult(IPAddress ipAddress, GeoLookupResult result) {
        var options = new MemoryCacheEntryOptions();
        options.AbsoluteExpirationRelativeToNow = ResultsCacheLifetime;
        options.Size = 1;

        ResultsCache.Set(ipAddress, result, options);
    }

    private static MemoryCache CreateResultsCache() {
        var options = new MemoryCacheOptions();
        options.SizeLimit = ResultsCacheSizeLimit;

        return new MemoryCache(options);
    }
}
