using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.GeoIP.Models;
using N3O.Umbraco.Lookups;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.GeoIP.Cloudflare;

public class CloudflareIPGeoLocationProvider : IIPGeoLocationProvider {
    private static readonly string CityHeader = "CF-IPCity";
    private static readonly string CountryHeader = "CF-IPCountry";
    private static readonly string RegionHeader = "CF-Region";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILookups _lookups;

    public CloudflareIPGeoLocationProvider(IHttpContextAccessor httpContextAccessor, ILookups lookups) {
        _httpContextAccessor = httpContextAccessor;
        _lookups = lookups;
    }

    public Task<GeoLookupResult> GeoLocateAsync(CancellationToken cancellationToken = default) {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null) {
            return Task.FromResult(GeoLookupResult.ForFailure());
        }

        var headers = httpContext.Request.Headers;
        var city = headers[CityHeader].FirstOrDefault();
        var countryCode = headers[CountryHeader].FirstOrDefault();
        var region = headers[RegionHeader].FirstOrDefault();

        if (!city.HasValue() && !countryCode.HasValue() && !region.HasValue()) {
            return Task.FromResult(GeoLookupResult.ForFailure());
        }

        var country = countryCode.HasValue() ? _lookups.GetAll<Country>().FindByCode(countryCode) : null;

        return Task.FromResult(GeoLookupResult.ForSuccess(country, city, region));
    }
}
