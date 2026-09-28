using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Giving.Allocations.Content;
using N3O.Umbraco.Giving.Allocations.Models;
using N3O.Umbraco.Lookups;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Giving.Allocations.Lookups;

public class SponsorshipComponentContent : UmbracoContent<SponsorshipComponentContent>, IHoldPricing {
    public bool Mandatory => GetValue(x => x.Mandatory);
    public PriceContent Price => Content().As<PriceContent>();

    [UmbracoProperty(AllocationsConstants.Aliases.SponsorshipScheme.Properties.PricingRules)]
    public IEnumerable<PricingRuleElement> PricingRules => GetNestedAs(x => x.PricingRules);
    
    [JsonIgnore]
    public Pricing Pricing {
        get {
            var price = Price?.Amount > 0 ? new Price(Price.Amount, Price.Locked) : null;
            
            if (price == null && PricingRules.None()) {
                return null;
            } else {
                return new Pricing(price, PricingRules);
            }
        }
    }
    
    [JsonIgnore]
    IPricing IHoldPricing.Pricing => Pricing;
    
    public SponsorshipScheme GetScheme() => Content().Parent.As<SponsorshipScheme>();
}

[Order(int.MinValue)]
public class ContentSponsorshipComponents : LookupsCollection<SponsorshipComponent> {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public ContentSponsorshipComponents(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
        _contentCache = contentCache;
        _umbracoContextAccessor = umbracoContextAccessor;

        _contentCache.Flushed += ContentCacheOnFlushed;
    }
    
    protected override bool CanReload() {
        return _contentCache.CanCache();
    }

    protected override Task<IReadOnlyList<SponsorshipComponent>> LoadAllAsync(CancellationToken cancellationToken) {
        var all = GetFromCache();
        
        return Task.FromResult(all);
    }

    private IReadOnlyList<SponsorshipComponent> GetFromCache() {
        List<SponsorshipComponentContent> content;
        
        if (_umbracoContextAccessor.TryGetUmbracoContext(out _)) {
            content = _contentCache.AllInDefaultCulture<SponsorshipComponentContent>().OrderBy(x => x.Content().Name).ToList();
        } else {
            content = [];
        }
        
        var lookups = content.Select(ToSponsorshipComponent).ToList();

        return lookups;
    }

    private SponsorshipComponent ToSponsorshipComponent(SponsorshipComponentContent sponsorshipComponentContent) {
        return new SponsorshipComponent(LookupContent.GetId(sponsorshipComponentContent.Content()),
                                        LookupContent.GetName(sponsorshipComponentContent.Content()),
                                        sponsorshipComponentContent.Content().Key,
                                        LookupContent.GetId(sponsorshipComponentContent.Content().Parent),
                                        sponsorshipComponentContent.Mandatory,
                                        sponsorshipComponentContent.Pricing);
    }

    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
}