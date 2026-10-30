using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Events.Criteria;
using N3O.Umbraco.Events.QueryFilters;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.Events;

public class EventsFinder : IEventsFinder {
    private readonly IContentCache _contentCache;
    private readonly ICultureAccessor _cultureAccessor;
    private readonly ILocalClock _localClock;
    private readonly EventQueryFilter _queryFilter;

    public EventsFinder(IContentCache contentCache,
                        ICultureAccessor cultureAccessor,
                        ILocalClock localClock,
                        EventQueryFilter queryFilter) {
        _contentCache = contentCache;
        _cultureAccessor = cultureAccessor;
        _localClock = localClock;
        _queryFilter = queryFilter;
    }

    public IReadOnlyList<T> FindEvents<T>(EventCriteria criteria) where T : IPublishedContent {
        var culture = _cultureAccessor.GetCulture();
        var all = _contentCache.All<T>().Where(x => x.IsInvariantOrHasCulture(culture)).ToList();
        var results = _queryFilter.Apply(all, criteria).ToList();

        return results;
    }
    
    public IReadOnlyList<T> FindOpenEvents<T>(EventCriteria criteria = null) where T : IPublishedContent {
        criteria ??= new EventCriteria();

        if (criteria.Date.HasValue()) {
            throw new Exception($"Criteria passed to {nameof(FindOpenEvents)} cannot specify {nameof(EventCriteria.Date)}");
        }
        
        criteria.Date = new Range<LocalDate?>(null, _localClock.GetLocalToday());

        return FindEvents<T>(criteria);
    }
}
