using Refit;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cdn.Cloudflare.Clients;

public interface IZonesApiClient {
    [Post("/zones/{zoneId}/purge_cache")]
    Task<ApiResponse<object>> PurgeCacheAsync(string zoneId, [Body] ApiPurgeCacheReq request);
}
