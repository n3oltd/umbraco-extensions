using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Financial;

public class CurrencyContent : UmbracoContent<CurrencyContent> {
    public string Code => Content().Name.ToUpperInvariant();
    public string Symbol => GetValue(x => x.Symbol);
    public bool IsBaseCurrency => GetValue(x => x.IsBaseCurrency);
}

[Order(int.MinValue)]
public class ContentCurrencies : LookupsCollection<Currency> {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public ContentCurrencies(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
        _contentCache = contentCache;
        _umbracoContextAccessor = umbracoContextAccessor;

        _contentCache.Flushed += ContentCacheOnFlushed;
    }
    
    protected override bool CanReload() {
        return _contentCache.CanCache();
    }

    protected override Task<IReadOnlyList<Currency>> LoadAllAsync(CancellationToken cancellationToken) {
        var all = GetFromCache();
        
        return Task.FromResult(all);
    }

    private IReadOnlyList<Currency> GetFromCache() {
        List<CurrencyContent> content;
        
        if (_umbracoContextAccessor.TryGetUmbracoContext(out _)) {
            content = _contentCache.AllInDefaultCulture<CurrencyContent>().OrderBy(x => x.Content().SortOrder).ToList();
        } else {
            content = [];
        }
        
        var lookups = content.Select(ToCurrency).ToList();

        return lookups;
    }

    private Currency ToCurrency(CurrencyContent currencyContent) {
        return new Currency(LookupContent.GetId(currencyContent.Content()),
                            LookupContent.GetName(currencyContent.Content()),
                            currencyContent.Content().Key,
                            currencyContent.Code,
                            currencyContent.Symbol,
                            2,
                            currencyContent.IsBaseCurrency);
    }

    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
}