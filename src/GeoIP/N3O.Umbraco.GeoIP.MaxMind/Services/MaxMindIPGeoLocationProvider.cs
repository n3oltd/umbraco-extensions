using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Exceptions;
using Microsoft.Extensions.Caching.Memory;
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

public class MaxMindIPGeoLocationProvider : IIPGeoLocationProvider {
    // SizeLimit caps cache growth — without it, every unique visitor IP accumulates forever
    private static readonly MemoryCache ResultsCache = new(new MemoryCacheOptions { SizeLimit = 10_000 });
    private static readonly TimeSpan ResultsCacheLifetime = TimeSpan.FromHours(12);

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

        var cachedResult = ResultsCache.Get<GeoLookupResult>(ipAddress);

        if (cachedResult != null) {
            return cachedResult;
        }

        var result = await LookupAsync(ipAddress);

        if (result.Success) {
            var entryOptions = new MemoryCacheEntryOptions();
            entryOptions.AbsoluteExpirationRelativeToNow = ResultsCacheLifetime;
            entryOptions.Size = 1;

            ResultsCache.Set(ipAddress, result, entryOptions);
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
        } catch (HttpException) {
        } catch (HttpRequestException) {
        } catch (InvalidOperationException) {
        } catch (OperationCanceledException) {
            // Nothing here passes cancellationToken down, so this is the MaxMind client's own timeout
        }

        return GeoLookupResult.ForFailure();
    }
}
