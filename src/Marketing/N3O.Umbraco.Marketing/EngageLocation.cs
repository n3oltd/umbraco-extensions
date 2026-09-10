using Umbraco.Engage.Infrastructure.Analytics.Processed;

namespace N3O.Umbraco.Marketing;

public class EngageLocation : ILocation {
    public string City { get; set; }
    public string Country { get; set; }
    public string County { get; set; }
    public string Province { get; set; }
}
