using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Giving.Allocations.Models;

public abstract class FundDimensionValueContent<TModel> : LookupContent<TModel>, IFundDimensionValue
    where TModel : IFundDimensionValue {
    public bool IsUnrestricted => GetValue(x => x.IsUnrestricted);
    public Guid? ContentId => Content().Key;
}

public class FundDimension1ValueContent : FundDimensionValueContent<FundDimension1ValueContent> { }
public class FundDimension2ValueContent : FundDimensionValueContent<FundDimension2ValueContent> { }
public class FundDimension3ValueContent : FundDimensionValueContent<FundDimension3ValueContent> { }
public class FundDimension4ValueContent : FundDimensionValueContent<FundDimension4ValueContent> { }

[Order(int.MinValue)]
public abstract class ContentFundDimensionValues<T, TContent> : LookupsCollection<T>
    where TContent : FundDimensionValueContent<TContent>
    where T : IFundDimensionValue  {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    protected ContentFundDimensionValues(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
        _contentCache = contentCache;
        _umbracoContextAccessor = umbracoContextAccessor;

        _contentCache.Flushed += ContentCacheOnFlushed;
    }
    
    protected override bool CanReload() {
        return _contentCache.CanCache();
    }

    protected override Task<IReadOnlyList<T>> LoadAllAsync(CancellationToken cancellationToken) {
        var all = GetFromCache();
        
        return Task.FromResult(all);
    }

    private IReadOnlyList<T> GetFromCache() {
        List<TContent> content;
        
        if (_umbracoContextAccessor.TryGetUmbracoContext(out _)) {
            content = _contentCache.AllInDefaultCulture<TContent>().OrderBy(x => x.Content().Name).ToList();
        } else {
            content = [];
        }
        
        var lookups = content.Select(GetFundDimensionValue).ToList();

        return lookups;
    }

    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
    
    protected abstract T GetFundDimensionValue(TContent content);
}

public class ContentFundDimension1Values : ContentFundDimensionValues<FundDimension1Value, FundDimension1ValueContent> {
    public ContentFundDimension1Values(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) 
        : base(contentCache, umbracoContextAccessor) { }
    
    protected override FundDimension1Value GetFundDimensionValue(FundDimension1ValueContent fundDimension1Value) {
        return new FundDimension1Value(LookupContent.GetId(fundDimension1Value.Content()),
                                       LookupContent.GetName(fundDimension1Value.Content()),
                                       fundDimension1Value.ContentId,
                                       fundDimension1Value.IsUnrestricted);
    }
}

public class ContentFundDimension2Values : ContentFundDimensionValues<FundDimension2Value, FundDimension2ValueContent> {
    public ContentFundDimension2Values(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) 
        : base(contentCache, umbracoContextAccessor) { }
    
    protected override FundDimension2Value GetFundDimensionValue(FundDimension2ValueContent fundDimension2Value) {
        return new FundDimension2Value(LookupContent.GetId(fundDimension2Value.Content()),
                                       LookupContent.GetName(fundDimension2Value.Content()),
                                       fundDimension2Value.ContentId,
                                       fundDimension2Value.IsUnrestricted);
    }
}

public class ContentFundDimension3Values : ContentFundDimensionValues<FundDimension3Value, FundDimension3ValueContent> {
    public ContentFundDimension3Values(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) 
        : base(contentCache, umbracoContextAccessor) { }
    
    protected override FundDimension3Value GetFundDimensionValue(FundDimension3ValueContent fundDimension3Value) {
        return new FundDimension3Value(LookupContent.GetId(fundDimension3Value.Content()),
                                       LookupContent.GetName(fundDimension3Value.Content()),
                                       fundDimension3Value.ContentId,
                                       fundDimension3Value.IsUnrestricted);
    }
}

public class ContentFundDimension4Values : ContentFundDimensionValues<FundDimension4Value, FundDimension4ValueContent> {
    public ContentFundDimension4Values(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) 
        : base(contentCache, umbracoContextAccessor) { }
    
    protected override FundDimension4Value GetFundDimensionValue(FundDimension4ValueContent fundDimension4Value) {
        return new FundDimension4Value(LookupContent.GetId(fundDimension4Value.Content()),
                                       LookupContent.GetName(fundDimension4Value.Content()),
                                       fundDimension4Value.ContentId,
                                       fundDimension4Value.IsUnrestricted);
    }
}
