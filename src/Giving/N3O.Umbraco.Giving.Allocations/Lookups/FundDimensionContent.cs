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

public abstract class FundDimensionContent<T, TValue> : LookupContent<T>, IFundDimension
    where T : FundDimensionContent<T, TValue>
    where TValue : FundDimensionValueContent<TValue> {
    protected FundDimensionContent(int index) {
        Index = index;
    }
    
    public bool IsActive => GetValue(x => x.IsActive);
    public IReadOnlyList<TValue> Options => Content().Children.As<TValue>();
    public int Index { get; }

    [JsonIgnore]
    IEnumerable<IFundDimensionValue> IFundDimension.Options => Options;
}

public class FundDimension1Content : FundDimensionContent<FundDimension1Content, FundDimension1ValueContent> {
    public FundDimension1Content() : base(1) { }
}

public class FundDimension2Content : FundDimensionContent<FundDimension2Content, FundDimension2ValueContent> {
    public FundDimension2Content() : base(2) { }
}

public class FundDimension3Content : FundDimensionContent<FundDimension3Content, FundDimension3ValueContent> {
    public FundDimension3Content() : base(3) { }
}

public class FundDimension4Content : FundDimensionContent<FundDimension4Content, FundDimension4ValueContent> {
    public FundDimension4Content() : base(4) { }
}

[Order(int.MinValue)]
public abstract class ContentFundDimensionContent<T, TContent, TValue> : LookupsCollection<T> 
    where TContent : FundDimensionContent<TContent, TValue> 
    where T : IFundDimension 
    where TValue : FundDimensionValueContent<TValue> {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    protected ContentFundDimensionContent(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
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
        
        var lookups = content.Select(ToFundDimension).ToList();

        return lookups;
    }

    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }

    protected abstract T ToFundDimension(TContent fundDimensionContent);
}

public class ContentFundDimension1Content :
    ContentFundDimensionContent<FundDimension1, FundDimension1Content, FundDimension1ValueContent> {
    public ContentFundDimension1Content(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) 
        : base(contentCache, umbracoContextAccessor) { }
    
    protected override FundDimension1 ToFundDimension(FundDimension1Content fundDimensionContent) {
        return new FundDimension1(LookupContent.GetId(fundDimensionContent.Content()),
                                  LookupContent.GetName(fundDimensionContent.Content()),
                                  fundDimensionContent.Content().Key,
                                  fundDimensionContent.IsActive,
                                  fundDimensionContent.Options.ToList());
    }
}
public class ContentFundDimension2Content :
    ContentFundDimensionContent<FundDimension2, FundDimension2Content, FundDimension2ValueContent> {
    public ContentFundDimension2Content(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) 
        : base(contentCache, umbracoContextAccessor) { }
    
    protected override FundDimension2 ToFundDimension(FundDimension2Content fundDimensionContent) {
        return new FundDimension2(LookupContent.GetId(fundDimensionContent.Content()),
                                  LookupContent.GetName(fundDimensionContent.Content()),
                                  fundDimensionContent.Content().Key,
                                  fundDimensionContent.IsActive,
                                  fundDimensionContent.Options.ToList());
    }
}
public class ContentFundDimension3Content :
    ContentFundDimensionContent<FundDimension3, FundDimension3Content, FundDimension3ValueContent> {
    public ContentFundDimension3Content(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) 
        : base(contentCache, umbracoContextAccessor) { }
    
    protected override FundDimension3 ToFundDimension(FundDimension3Content fundDimensionContent) {
        return new FundDimension3(LookupContent.GetId(fundDimensionContent.Content()),
                                  LookupContent.GetName(fundDimensionContent.Content()),
                                  fundDimensionContent.Content().Key,
                                  fundDimensionContent.IsActive,
                                  fundDimensionContent.Options.ToList());
    }
}
public class ContentFundDimension4Content :
    ContentFundDimensionContent<FundDimension4, FundDimension4Content, FundDimension4ValueContent> {
    public ContentFundDimension4Content(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) 
        : base(contentCache, umbracoContextAccessor) { }
    
    protected override FundDimension4 ToFundDimension(FundDimension4Content fundDimensionContent) {
        return new FundDimension4(LookupContent.GetId(fundDimensionContent.Content()),
                                  LookupContent.GetName(fundDimensionContent.Content()),
                                  fundDimensionContent.Content().Key,
                                  fundDimensionContent.IsActive,
                                  fundDimensionContent.Options.ToList());
    }
}