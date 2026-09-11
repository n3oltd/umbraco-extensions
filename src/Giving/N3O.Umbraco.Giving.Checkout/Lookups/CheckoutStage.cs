using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Checkout.Content;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Giving.Checkout.Lookups;

public class CheckoutStage : NamedLookup {
    private readonly Func<Entities.Checkout, bool> _isRequired;
    private readonly Func<Entities.Checkout, bool> _isComplete;
    private readonly Func<IContentCache, IUmbracoContent> _getPage;

    public CheckoutStage(string id,
                         string name,
                         string transactionIdPrefix,
                         Func<Entities.Checkout, bool> isRequired,
                         Func<Entities.Checkout, bool> isComplete,
                         int order,
                         bool canRevisit,
                         Func<IContentCache, IUmbracoContent> getPage)
        : base(id, name) {
        _isRequired = isRequired;
        _isComplete = isComplete;
        _getPage = getPage;
        TransactionIdPrefix = transactionIdPrefix;
        Order = order;
        CanRevisit = canRevisit;
    }

    public string TransactionIdPrefix { get; }
    public int Order { get; }
    public bool CanRevisit { get; set; }
    
    public bool IsComplete(Entities.Checkout checkout) => _isComplete(checkout);
    
    public bool IsRequired(Entities.Checkout checkout) => _isRequired(checkout);
    
    public string GetUrl(IContentCache contentCache) => _getPage(contentCache)?.Content()?.AbsoluteUrl();
}

public class CheckoutStagesCollection : LookupsCollection<CheckoutStage> {
    protected override Task<IReadOnlyList<CheckoutStage>> LoadAllAsync(CancellationToken cancellationToken) {
        var checkoutStages = new List<CheckoutStage>();
        
        checkoutStages.Add(CheckoutStages.Account);
        checkoutStages.Add(CheckoutStages.Donation);
        checkoutStages.Add(CheckoutStages.RegularGiving);

        return Task.FromResult<IReadOnlyList<CheckoutStage>>(checkoutStages);
    }
}

public static class CheckoutStages {
    public static readonly CheckoutStage Account = new("account",
                                                       "Account",
                                                       null,
                                                       _ => true,
                                                       c => c.Account?.IsComplete == true,
                                                       0,
                                                       true,
                                                       c => c.Single<CheckoutAccountPageContent>());

    public static readonly CheckoutStage Donation = new("donation",
                                                        "Donation",
                                                        "DN",
                                                        c => c.Donation.IsRequired,
                                                        c => c.Donation.IsComplete,
                                                        20,
                                                        false,
                                                        c => c.Single<CheckoutDonationPageContent>());
    
    public static readonly CheckoutStage RegularGiving = new("regularGiving",
                                                             "Regular Giving",
                                                             "RG",
                                                             c => c.RegularGiving.IsRequired,
                                                             c => c.RegularGiving.IsComplete,
                                                             10,
                                                             false,
                                                             c => c.Single<CheckoutRegularGivingPageContent>());
}
