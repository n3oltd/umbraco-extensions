using N3O.Umbraco.Extensions;
using N3O.Umbraco.GeoIP;
using System.Collections.Generic;
using Umbraco.Engage.Data.Analytics.Collection.Pageview;
using Umbraco.Engage.Infrastructure.Analytics.Processed;
using Umbraco.Engage.Infrastructure.Analytics.Processing.Extractors;

namespace N3O.Umbraco.Marketing;

public class EngageLocationExtractor : IRawPageviewLocationExtractor {
    private static readonly int MaxColumnWidth = 100;

    private readonly IReadOnlyList<IIPGeoLocationProvider> _ipGeoLocationProviders;

    public EngageLocationExtractor(IEnumerable<IIPGeoLocationProvider> ipGeoLocationProviders) {
        _ipGeoLocationProviders = ipGeoLocationProviders.ApplyAttributeOrdering();
    }

    public ILocation Extract(IRawPageview rawPageview) {
        foreach (var ipGeoLocationProvider in _ipGeoLocationProviders) {
            var geoLookupResult = ipGeoLocationProvider.GeoLocateAsync().GetAwaiter().GetResult();

            if (!geoLookupResult.Success) {
                continue;
            }

            var location = new EngageLocation();
            location.City = WithinColumnWidth(geoLookupResult.City);
            location.Country = geoLookupResult.Country?.Name;
            location.County = Location.Unknown.County;
            location.Province = WithinColumnWidth(geoLookupResult.Province);

            return location;
        }

        return null;
    }

    // Engage stores city and province as nvarchar(100), and a longer value faults its processing pipeline.
    private static string WithinColumnWidth(string value) {
        return value?.Length > MaxColumnWidth ? null : value;
    }
}
