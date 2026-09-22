using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

public class DonationButtonOfferingElementKindDataSource : ElementKindDataSource {
    private readonly ILookups _lookups;
    
    public DonationButtonOfferingElementKindDataSource(ILookups lookups) : base(lookups) {
        _lookups = lookups;
    }
    
    public override  string Name => "Donation Button (Offering) Elements";
    public override  string Description => "Data source for offering donation button elements";
    public override  string Icon => "icon-categories";
    
    protected override string GetDescription(Element lookup) {
        var offering = _lookups.FindById<Offering>(GetOfferingId(lookup));

        if (offering == null) {
            return "[unavailable]";
        }
        
        var campaign = _lookups.FindById<Campaign>(offering.CampaignId);
        
        if (campaign == null) {
            return "[unavailable]";
        }
        
        return $"Campaign: {campaign.Name}";
    }

    protected override ElementKind Kind => ElementKind.DonationButtonOffering;
}