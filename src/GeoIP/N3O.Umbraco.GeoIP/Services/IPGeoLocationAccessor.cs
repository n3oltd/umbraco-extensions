using N3O.Umbraco.Extensions;
using N3O.Umbraco.GeoIP.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.GeoIP;

public class IPGeoLocationAccessor : IIPGeoLocationAccessor {
    private readonly IReadOnlyList<IIPGeoLocationProvider> _ipGeoLocationProviders;

    public IPGeoLocationAccessor(IEnumerable<IIPGeoLocationProvider> ipGeoLocationProviders) {
        _ipGeoLocationProviders = ipGeoLocationProviders.ApplyAttributeOrdering();
    }

    public async Task<GeoLookupResult> GeoLocateAsync(CancellationToken cancellationToken = default) {
        foreach (var ipGeoLocationProvider in _ipGeoLocationProviders) {
            var geoLookupResult = await ipGeoLocationProvider.GeoLocateAsync(cancellationToken);

            if (geoLookupResult.Success) {
                return geoLookupResult;
            }
        }

        return GeoLookupResult.ForFailure();
    }
}
