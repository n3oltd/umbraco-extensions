using N3O.Umbraco.Entities;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Checkout.Models;
using N3O.Umbraco.Payments.Entities;
using N3O.Umbraco.References;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;

namespace N3O.Umbraco.Giving.Checkout.Entities;

public partial class Checkout : Entity, IPaymentsFlow {
    public RevisionId CartRevisionId { get; private set; }
    public Reference Reference { get; private set; }
    public Currency Currency { get; private set; }
    public CheckoutProgress Progress { get; private set; }
    public CheckoutAccount Account { get; private set; }
    public DonationCheckout Donation { get; private set; }
    public RegularGivingCheckout RegularGiving { get; private set; }
    public IPAddress RemoteIp { get; private set; }
    public JObject Tags { get; private set; }

    public bool IsComplete => Progress.RequiredStages.All(x => x.IsComplete(this));
}