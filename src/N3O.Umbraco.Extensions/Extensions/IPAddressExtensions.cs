using System.Net;

namespace N3O.Umbraco.Extensions;

public static class IPAddressExtensions {
    public static IPAddress UnmapIPv4(this IPAddress ipAddress) {
        if (ipAddress != null && ipAddress.IsIPv4MappedToIPv6) {
            return ipAddress.MapToIPv4();
        } else {
            return ipAddress;
        }
    }
}
