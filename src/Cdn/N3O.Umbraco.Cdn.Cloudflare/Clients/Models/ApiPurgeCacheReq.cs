using System.Collections.Generic;

namespace N3O.Umbraco.Cdn.Cloudflare.Clients;

public class ApiPurgeCacheReq {
    public IEnumerable<string> Tags { get; set; }
}
