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

public class FeedbackSchemeContent :
    LookupContent<FeedbackSchemeContent>, IHoldAllowedGivingTypes, IHoldFundDimensionOptions, IHoldPricing {
    public IEnumerable<GivingType> AllowedGivingTypes => GetValue(x => x.AllowedGivingTypes);
    public IEnumerable<FeedbackCustomFieldDefinitionElement> CustomFields => GetNestedAs(x => x.CustomFields);

    [UmbracoProperty(AllocationsConstants.Aliases.FeedbackScheme.Properties.Dimension1)]
    public IEnumerable<FundDimension1Value> Dimension1 => GetPickedAs(x => x.Dimension1);
    
    [UmbracoProperty(AllocationsConstants.Aliases.FeedbackScheme.Properties.Dimension2)]
    public IEnumerable<FundDimension2Value> Dimension2 => GetPickedAs(x => x.Dimension2);
    
    [UmbracoProperty(AllocationsConstants.Aliases.FeedbackScheme.Properties.Dimension3)]
    public IEnumerable<FundDimension3Value> Dimension3 => GetPickedAs(x => x.Dimension3);
    
    [UmbracoProperty(AllocationsConstants.Aliases.FeedbackScheme.Properties.Dimension4)]
    public IEnumerable<FundDimension4Value> Dimension4 => GetPickedAs(x => x.Dimension4);
    
    public PriceContent Price => Content().As<PriceContent>();
    
    [UmbracoProperty(AllocationsConstants.Aliases.FeedbackScheme.Properties.PricingRules)]
    public IEnumerable<PricingRuleElement> PricingRules => GetNestedAs(x => x.PricingRules);
    
    [JsonIgnore]
    public FundDimensionOptions FundDimensionOptions => new(Dimension1, Dimension2, Dimension3, Dimension4);

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
    IFundDimensionOptions IHoldFundDimensionOptions.FundDimensionOptions => FundDimensionOptions;
    
    [JsonIgnore]
    IPricing IHoldPricing.Pricing => Pricing;
}

[Order(int.MinValue)]
public class ContentFeedbackSchemes : LookupsCollection<FeedbackScheme> {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public ContentFeedbackSchemes(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
        _contentCache = contentCache;
        _umbracoContextAccessor = umbracoContextAccessor;

        _contentCache.Flushed += ContentCacheOnFlushed;
    }
    
    protected override bool CanReload() {
        return _contentCache.CanCache();
    }

    protected override Task<IReadOnlyList<FeedbackScheme>> LoadAllAsync(CancellationToken cancellationToken) {
        var all = GetFromCache();
        
        return Task.FromResult(all);
    }

    private IReadOnlyList<FeedbackScheme> GetFromCache() {
        List<FeedbackSchemeContent> content;
        
        if (_umbracoContextAccessor.TryGetUmbracoContext(out _)) {
            content = _contentCache.AllInDefaultCulture<FeedbackSchemeContent>().OrderBy(x => x.Content().Name).ToList();
        } else {
            content = [];
        }
        
        var lookups = content.Select(ToFeedbackScheme).ToList();

        return lookups;
    }

    private FeedbackScheme ToFeedbackScheme(FeedbackSchemeContent feedbackSchemeContent) {
        return new FeedbackScheme(LookupContent.GetId(feedbackSchemeContent.Content()),
                                  LookupContent.GetName(feedbackSchemeContent.Content()),
                                  feedbackSchemeContent.Content().Key,
                                  feedbackSchemeContent.AllowedGivingTypes,
                                  feedbackSchemeContent.FundDimensionOptions,
                                  feedbackSchemeContent.Pricing,
                                  feedbackSchemeContent.CustomFields
                                                       .OrEmpty()
                                                       .Select(x => new FeedbackCustomFieldDefinition(x)));
    }

    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
}