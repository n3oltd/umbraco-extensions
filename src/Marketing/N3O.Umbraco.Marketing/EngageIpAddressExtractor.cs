using Microsoft.AspNetCore.Http;
using N3O.Umbraco.Context;
using Umbraco.Engage.Infrastructure.Analytics.Collection.Extractors;

namespace N3O.Umbraco.Marketing;

public class EngageIpAddressExtractor : IHttpContextIpAddressExtractor {
    private readonly IRemoteIpAddressAccessor _remoteIpAddressAccessor;

    public EngageIpAddressExtractor(IRemoteIpAddressAccessor remoteIpAddressAccessor) {
        _remoteIpAddressAccessor = remoteIpAddressAccessor;
    }

    public string ExtractIpAddress(HttpContext context) {
        var ipAddress = _remoteIpAddressAccessor.GetRemoteIpAddress();

        return ipAddress?.ToString();
    }
}
