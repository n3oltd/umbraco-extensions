using N3O.Umbraco.Context;
using N3O.Umbraco.Entities;
using N3O.Umbraco.Financial;
using N3O.Umbraco.Giving.Checkout.Lookups;
using N3O.Umbraco.Giving.Checkout.Models;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.References;
using System.Net;
using System.Threading.Tasks;

namespace N3O.Umbraco.Giving.Checkout.Entities;

public partial class Checkout {
    public static async Task<Checkout> CreateAsync(ICounters counters,
                                                   ICultureAccessor cultureAccessor,
                                                   ILookups lookups,
                                                   IRemoteIpAddressAccessor remoteIpAddressAccessor,
                                                   EntityId id,
                                                   Cart.Entities.Cart cart) {
        var checkout = Create<Checkout>(id);

        checkout.CartRevisionId = cart.RevisionId;
        checkout.Reference = await counters.NextAsync<CheckoutReferenceType>();
        checkout.Culture = cultureAccessor.GetCulture();
        checkout.Currency = cart.Currency;
        checkout.Donation = new DonationCheckout(cart.Donation.Allocations, cart.Currency);
        checkout.RegularGiving = new RegularGivingCheckout(cart.RegularGiving.Allocations, cart.Currency);
        checkout.Progress = new CheckoutProgress(lookups, checkout);
        checkout.RemoteIp = remoteIpAddressAccessor.GetRemoteIpAddress();

        return checkout;
    }
    
    public static Checkout Create(RevisionId revisionId,
                                  RevisionId cartRevisionId,
                                  Reference reference,
                                  Currency currency,
                                  CheckoutAccount account,
                                  DonationCheckout donationCheckout,
                                  RegularGivingCheckout regularGivingCheckout,
                                  CheckoutProgress checkoutProgress,
                                  IPAddress ipAddress) {
        var checkout = Create<Checkout>(revisionId.Id);

        checkout.CartRevisionId = cartRevisionId;
        checkout.Reference = reference;
        checkout.Currency = currency;
        checkout.Account = account;
        checkout.Donation = donationCheckout;
        checkout.RegularGiving = regularGivingCheckout;
        checkout.Progress = checkoutProgress;
        checkout.RemoteIp = ipAddress;

        return checkout;
    }
}
