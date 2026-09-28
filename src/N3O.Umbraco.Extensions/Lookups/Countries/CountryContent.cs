using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Lookups;

public class CountryContent : UmbracoContent<CountryContent> {
    public string Iso2Code => GetValue(x => x.Iso2Code);
    public string Iso3Code => GetValue(x => x.Iso3Code);
    [UmbracoProperty("diallingCode")]
    public int DialingCode => GetConvertedValue<string, int>(x => x.DialingCode, int.Parse);
    public bool LocalityOptional => GetValue(x => x.LocalityOptional);
    public bool PostalCodeOptional => GetValue(x => x.PostalCodeOptional);
}

[Order(int.MinValue)]
public class ContentCountries : LookupsCollection<Country> {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public ContentCountries(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
        _contentCache = contentCache;
        _umbracoContextAccessor = umbracoContextAccessor;

        _contentCache.Flushed += ContentCacheOnFlushed;
    }
    
    protected override bool CanReload() {
        return _contentCache.CanCache();
    }

    protected override Task<IReadOnlyList<Country>> LoadAllAsync(CancellationToken cancellationToken) {
        var all = GetFromCache();
        
        return Task.FromResult(all);
    }

    private IReadOnlyList<Country> GetFromCache() {
        List<CountryContent> content;
        
        if (_umbracoContextAccessor.TryGetUmbracoContext(out _)) {
            content = _contentCache.AllInDefaultCulture<CountryContent>().OrderBy(x => x.Content().Name).ToList();
        } else {
            content = [];
        }
        
        var lookups = content.Select(ToCountry).ToList();

        return lookups;
    }

    private Country ToCountry(CountryContent countryContent) {
        return new Country(LookupContent.GetId(countryContent.Content()),
                           LookupContent.GetName(countryContent.Content()),
                           countryContent.Content().Key,
                           countryContent.Iso2Code,
                           countryContent.Iso3Code,
                           countryContent.DialingCode,
                           countryContent.LocalityOptional,
                           countryContent.PostalCodeOptional);
    }

    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
}