using N3O.Umbraco.GeoIP;
using Umbraco.Engage.Data.Analytics.Collection.Pageview;
using Umbraco.Engage.Infrastructure.Analytics.Processed;
using Umbraco.Engage.Infrastructure.Analytics.Processing.Extractors;

namespace N3O.Umbraco.Marketing;

public class EngageLocationExtractor : IRawPageviewLocationExtractor {
    private static readonly int MaxColumnWidth = 100;

    private readonly IIPGeoLocationProvider _ipGeoLocationProvider;

    public EngageLocationExtractor(IIPGeoLocationProvider ipGeoLocationProvider) {
        _ipGeoLocationProvider = ipGeoLocationProvider;
    }

    public ILocation Extract(IRawPageview rawPageview) {
        // Engage extracts synchronously, so the lookup is blocked on rather than awaited. It runs on the request
        // that is being recorded, and the provider caches per address, so it reaches the service once per visitor.
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
