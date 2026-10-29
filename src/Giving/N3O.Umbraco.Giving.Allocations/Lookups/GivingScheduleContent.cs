using N3O.Umbraco.Attributes;
using N3O.Umbraco.Content;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Giving.Allocations.Lookups;

public class GivingScheduleContent : UmbracoContent<GivingScheduleContent> { }


[Order(int.MinValue)]
public class ContentGivingSchedules : LookupsCollection<GivingSchedule> {
    private readonly IContentCache _contentCache;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public ContentGivingSchedules(IContentCache contentCache, IUmbracoContextAccessor umbracoContextAccessor) {
        _contentCache = contentCache;
        _umbracoContextAccessor = umbracoContextAccessor;

        _contentCache.Flushed += ContentCacheOnFlushed;
    }
    
    protected override bool CanReload() {
        return _contentCache.CanCache();
    }

    protected override Task<IReadOnlyList<GivingSchedule>> LoadAllAsync(CancellationToken cancellationToken) {
        var all = GetFromCache();
        
        return Task.FromResult(all);
    }

    private IReadOnlyList<GivingSchedule> GetFromCache() {
        List<GivingScheduleContent> content;
        
        if (_umbracoContextAccessor.TryGetUmbracoContext(out _)) {
            content = _contentCache.AllInDefaultCulture<GivingScheduleContent>().OrderBy(x => x.Content().Name).ToList();
        } else {
            content = [];
        }
        
        var lookups = content.Select(ToGivingSchedule).ToList();

        return lookups;
    }

    private GivingSchedule ToGivingSchedule(GivingScheduleContent givingScheduleContent) {
        return new GivingSchedule(LookupContent.GetId(givingScheduleContent.Content()),
                                  LookupContent.GetName(givingScheduleContent.Content()),
                                  givingScheduleContent.Content().Key);
    }

    private void ContentCacheOnFlushed(object sender, EventArgs e) {
        MarkStale();
    }
}