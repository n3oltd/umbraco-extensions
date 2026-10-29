using N3O.Umbraco.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Lookups;

public class UmbracoLookupsCollection<T> : LookupsCollection<T> where T : LookupContent<T> {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public UmbracoLookupsCollection(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
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
        if (_umbracoContextAccessor.TryGetUmbracoContext(out _)) {
            var items = _contentCache.AllInDefaultCulture<T>().OrderBy(x => x.Content().SortOrder).ToList();

            return items;
        } else {
            return new List<T>();
        }
    }
    
    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
}
