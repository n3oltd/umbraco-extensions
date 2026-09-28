using N3O.Umbraco.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Lookups;

public class UmbracoLookupsCollection<T> : LookupsCollection<T> where T : LookupContent<T> {
    private readonly IContentCache _contentCache;

    public UmbracoLookupsCollection(IContentCache contentCache) {
        _contentCache = contentCache;

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
        return _contentCache.AllInDefaultCulture<T>().OrderBy(x => x.Content().SortOrder).ToList();
    }
    
    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
}
