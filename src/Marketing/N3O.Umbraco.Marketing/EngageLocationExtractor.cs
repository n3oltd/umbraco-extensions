using N3O.Umbraco.GeoIP;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Engage.Data.Analytics.Collection.Pageview;
using Umbraco.Engage.Infrastructure.Analytics.Processed;
using Umbraco.Engage.Infrastructure.Analytics.Processing.Extractors;

namespace N3O.Umbraco.Marketing;

public class EngageLocationExtractor : IRawPageviewLocationExtractor {
    private static readonly int MaxColumnWidth = 100;

    private readonly IIPGeoLocationProvider _ipGeoLocationProvider;

    // The last registration is the one a single IIPGeoLocationProvider dependency resolves to, so location and the
    // geo IP default currency come from the same provider.
    public EngageLocationExtractor(IEnumerable<IIPGeoLocationProvider> ipGeoLocationProviders) {
        _ipGeoLocationProvider = ipGeoLocationProviders.LastOrDefault();
    }

    public ILocation Extract(IRawPageview rawPageview) {
        if (_ipGeoLocationProvider == null) {
            return null;
        }

        var geoLookupResult = _ipGeoLocationProvider.GeoLocateAsync().GetAwaiter().GetResult();

        if (!geoLookupResult.Success) {
            return null;
        }

        var location = new EngageLocation();
        location.City = WithinColumnWidth(geoLookupResult.City);
        location.Country = geoLookupResult.Country?.Name;
        location.County = Location.Unknown.County;
        location.Province = WithinColumnWidth(geoLookupResult.Province);

        return location;
    }

    // Engage stores city and province as nvarchar(100), and a longer value faults its processing pipeline.
    private static string WithinColumnWidth(string value) {
        return value?.Length > MaxColumnWidth ? null : value;
    }
}
