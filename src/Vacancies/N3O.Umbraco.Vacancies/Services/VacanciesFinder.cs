using N3O.Umbraco.Content;
using N3O.Umbraco.Context;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Vacancies.Criteria;
using N3O.Umbraco.Vacancies.QueryFilters;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace N3O.Umbraco.Vacancies;

public class VacanciesFinder : IVacanciesFinder {
    private readonly IContentCache _contentCache;
    private readonly ICultureAccessor _cultureAccessor;
    private readonly ILocalClock _localClock;
    private readonly VacancyQueryFilter _queryFilter;

    public VacanciesFinder(IContentCache contentCache,
                           ICultureAccessor cultureAccessor,
                           ILocalClock localClock,
                           VacancyQueryFilter queryFilter) {
        _contentCache = contentCache;
        _cultureAccessor = cultureAccessor;
        _localClock = localClock;
        _queryFilter = queryFilter;
    }

    public IReadOnlyList<T> FindVacancies<T>(VacancyCriteria criteria = null) where T : IPublishedContent {
        var culture = _cultureAccessor.GetCulture();
        var all = _contentCache.All<T>().Where(x => x.IsInvariantOrHasCulture(culture)).ToList();
        var results = criteria == null ? all : _queryFilter.Apply(all, criteria).ToList();

        return results;
    }
    
    public IReadOnlyList<T> FindOpenVacancies<T>(VacancyCriteria criteria = null) where T : IPublishedContent {
        criteria ??= new VacancyCriteria();

        if (criteria.ClosingDate.HasValue()) {
            throw new Exception($"Criteria passed to {nameof(FindOpenVacancies)} cannot specify {nameof(VacancyCriteria.ClosingDate)}");
        }
        
        criteria.ClosingDate = new Range<LocalDate?>(null, _localClock.GetLocalToday());

        return FindVacancies<T>(criteria);
    }
}
