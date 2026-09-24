using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
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

public class SponsorshipSchemeContent :
    UmbracoContent<SponsorshipSchemeContent>, IHoldAllowedGivingTypes, IHoldFundDimensionOptions {
    public IEnumerable<GivingType> AllowedGivingTypes => GetValue(x => x.AllowedGivingTypes);
    public IEnumerable<SponsorshipDuration> AllowedDurations => GetValue(x => x.AllowedDurations);

    [UmbracoProperty(AllocationsConstants.Aliases.SponsorshipScheme.Properties.Dimension1)]
    public IEnumerable<FundDimension1Value> Dimension1 => GetPickedAs(x => x.Dimension1);
    
    [UmbracoProperty(AllocationsConstants.Aliases.SponsorshipScheme.Properties.Dimension2)]
    public IEnumerable<FundDimension2Value> Dimension2 => GetPickedAs(x => x.Dimension2);
    
    [UmbracoProperty(AllocationsConstants.Aliases.SponsorshipScheme.Properties.Dimension3)]
    public IEnumerable<FundDimension3Value> Dimension3 => GetPickedAs(x => x.Dimension3);
    
    [UmbracoProperty(AllocationsConstants.Aliases.SponsorshipScheme.Properties.Dimension4)]
    public IEnumerable<FundDimension4Value> Dimension4 => GetPickedAs(x => x.Dimension4);
    
    public IEnumerable<SponsorshipComponent> Components => Content().Children.As<SponsorshipComponent>();

    [JsonIgnore]
    public FundDimensionOptions FundDimensionOptions => new(Dimension1, Dimension2, Dimension3, Dimension4);

    [JsonIgnore]
    IFundDimensionOptions IHoldFundDimensionOptions.FundDimensionOptions => FundDimensionOptions;
}

[Order(int.MinValue)]
public class ContentSponsorshipSchemes : LookupsCollection<SponsorshipScheme> {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public ContentSponsorshipSchemes(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
        _contentCache = contentCache;
        _umbracoContextAccessor = umbracoContextAccessor;

        _contentCache.Flushed += ContentCacheOnFlushed;
    }
    
    protected override bool CanReload() {
        return _contentCache.CanCache();
    }

    protected override Task<IReadOnlyList<SponsorshipScheme>> LoadAllAsync(CancellationToken cancellationToken) {
        var all = GetFromCache();
        
        return Task.FromResult(all);
    }

    private IReadOnlyList<SponsorshipScheme> GetFromCache() {
        List<SponsorshipSchemeContent> content;
        
        if (_umbracoContextAccessor.TryGetUmbracoContext(out _)) {
            content = _contentCache.AllInDefaultCulture<SponsorshipSchemeContent>().OrderBy(x => x.Content().Name).ToList();
        } else {
            content = [];
        }
        
        var lookups = content.Select(ToSponsorshipScheme).ToList();

        return lookups;
    }

    private SponsorshipScheme ToSponsorshipScheme(SponsorshipSchemeContent sponsorshipSchemeContent) {
        return new SponsorshipScheme(LookupContent.GetId(sponsorshipSchemeContent.Content()),
                                     LookupContent.GetName(sponsorshipSchemeContent.Content()),
                                     sponsorshipSchemeContent.Content().Key,
                                     sponsorshipSchemeContent.AllowedGivingTypes,
                                     sponsorshipSchemeContent.AllowedDurations,
                                     sponsorshipSchemeContent.FundDimensionOptions,
                                     sponsorshipSchemeContent.Components,
                                     null);
    }

    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
}