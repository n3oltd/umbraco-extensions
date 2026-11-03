using N3O.Umbraco.Lookups;

namespace N3O.Umbraco.Giving.Allocations.Lookups;

public class DonationPlan : NamedLookup {
    public DonationPlan(string id, string name, int donations) : base(id, name) {
        Donations = donations;
    }

    public int Donations { get; }
}

public class DonationPlans : StaticLookupsCollection<DonationPlan> {
    public static readonly DonationPlan TwoDonations = new("_2", "2 Donations", 2);
    public static readonly DonationPlan ThreeDonations = new("_3", "3 Donations", 3);
    public static readonly DonationPlan FourDonations = new("_4", "4 Donations", 4);
    public static readonly DonationPlan SixDonations = new("_6", "6 Donations", 6);
    public static readonly DonationPlan TwelveDonations = new("_12", "12 Donations", 12);
    public static readonly DonationPlan EighteenDonations = new("_18", "18 Donations", 18);
    public static readonly DonationPlan TwentyFourDonations = new("_24", "24 Donations", 24);
}
