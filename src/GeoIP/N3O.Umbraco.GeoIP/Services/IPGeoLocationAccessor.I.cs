using N3O.Umbraco.GeoIP.Models;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.GeoIP;

public interface IIPGeoLocationAccessor {
    Task<GeoLookupResult> GeoLocateAsync(CancellationToken cancellationToken = default);
}
