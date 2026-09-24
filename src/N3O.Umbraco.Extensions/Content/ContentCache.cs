using N3O.Umbraco.Extensions;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Content;

public class ContentCache : IContentCache {
    private readonly IContentLocator _contentLocator;
    private readonly ILocalizationService _localizationService;
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly ConcurrentDictionary<string, object> _typedStore = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly ConcurrentDictionary<string, object> _untypedStore = new(StringComparer.InvariantCultureIgnoreCase);
    private DateTime _flushedAt = DateTime.MinValue;
    private IReadOnlyList<ILanguage> _languages;

    public ContentCache(IContentLocator contentLocator,
                        ILocalizationService localizationService,
                        ICoreScopeProvider scopeProvider,
                        IUmbracoContextAccessor umbracoContextAccessor,
                        IVariationContextAccessor variationContextAccessor) {
        _contentLocator = contentLocator;
        _localizationService = localizationService;
        _scopeProvider = scopeProvider;
        _umbracoContextAccessor = umbracoContextAccessor;
        _variationContextAccessor = variationContextAccessor;
    }

    public IReadOnlyList<T> All<T>(Func<T, bool> predicate = null) {
        var all = GetTyped<T>(GetCultures());

        IReadOnlyList<T> res;

        if (predicate == null) {
            res = all;
        } else {
            res = all.Where(predicate).ToList();
        }

        return res;
    }

    public IReadOnlyList<IPublishedContent> All(string contentTypeAlias,
                                                Func<IPublishedContent, bool> predicate = null) {
        var all = Get(_untypedStore,
                      contentTypeAlias,
                      culture => _contentLocator.AllInCulture(contentTypeAlias, culture),
                      GetCultures());

        IReadOnlyList<IPublishedContent> res;

        if (predicate == null) {
            res = all;
        } else {
            res = all.Where(predicate).ToList();
        }

        return res;
    }

    public IReadOnlyList<T> AllInDefaultCulture<T>() {
        return GetTyped<T>([GetDefaultCulture(GetLanguages())]);
    }

    public bool CanCache() {
        return _scopeProvider.Context == null &&
               _umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) &&
               !umbracoContext.InPreviewMode &&
               umbracoContext.ObjectCreated.ToUniversalTime() > _flushedAt;
    }

    public void Flush() {
        _flushedAt = DateTime.UtcNow;
        _languages = null;
        _typedStore.Clear();
        _untypedStore.Clear();

        Flushed?.Invoke(this, EventArgs.Empty);
    }

    public T Single<T>(Func<T, bool> predicate = null) {
        return All(predicate).SingleOrDefault();
    }

    public IPublishedContent Single(string contentTypeAlias, Func<IPublishedContent, bool> predicate = null) {
        return All(contentTypeAlias, predicate).SingleOrDefault();
    }

    public event EventHandler Flushed;

    private IReadOnlyList<T> Get<T>(ConcurrentDictionary<string, object> store,
                                    string value,
                                    Func<string, IReadOnlyList<T>> locate,
                                    IEnumerable<string> cultures) {
        IReadOnlyList<T> content = [];

        foreach (var culture in cultures) {
            content = GetOrLocate(store, value, culture, locate);

            if (content.Any()) {
                break;
            }
        }

        return content;
    }

    private string GetCulture(IEnumerable<ILanguage> languages, string isoCode) {
        return languages.SingleOrDefault(x => x.IsoCode.EqualsInvariant(isoCode))?.IsoCode;
    }

    private IReadOnlyList<string> GetCultures() {
        var languages = GetLanguages();
        var defaultCulture = GetDefaultCulture(languages);
        var variationContext = _variationContextAccessor.VariationContext;
        var culture = GetCulture(languages, variationContext?.Culture) ?? "";
        var cultures = new List<string>();

        cultures.Add(culture);

        while (variationContext != null && !culture.EqualsInvariant(defaultCulture)) {
            var fallbackCulture = GetCulture(languages, GetFallbackIsoCode(languages, culture));

            if (fallbackCulture == null || cultures.Contains(fallbackCulture)) {
                culture = defaultCulture;
            } else {
                culture = fallbackCulture;
            }

            cultures.Add(culture);
        }

        return cultures;
    }

    private string GetDefaultCulture(IEnumerable<ILanguage> languages) {
        return languages.SingleOrDefault(x => x.IsDefault)?.IsoCode ?? "";
    }

    private string GetFallbackIsoCode(IEnumerable<ILanguage> languages, string culture) {
        return languages.SingleOrDefault(x => x.IsoCode.EqualsInvariant(culture))?.FallbackIsoCode;
    }

    private IReadOnlyList<ILanguage> GetLanguages() {
        var languages = _languages;

        if (languages == null) {
            languages = _localizationService.GetAllLanguages().ToList();

            if (languages.Any()) {
                _languages = languages;
            }
        }

        return languages;
    }

    private IReadOnlyList<T> GetOrLocate<T>(ConcurrentDictionary<string, object> store,
                                            string value,
                                            string culture,
                                            Func<string, IReadOnlyList<T>> locate) {
        var cacheKey = CacheKey.Generate<ContentCache>(value, culture);

        if (!InPreviewMode() && store.TryGetValue(cacheKey, out var stored)) {
            return (IReadOnlyList<T>) stored;
        } else {
            var located = locate(culture);

            if (CanCache()) {
                return (IReadOnlyList<T>) store.GetOrAdd(cacheKey, located);
            } else {
                return located;
            }
        }
    }

    private IReadOnlyList<T> GetTyped<T>(IEnumerable<string> cultures) {
        // Not AliasHelper<T>.ContentTypeAlias() as need to distinguish T and TContent : UmbracoContent<TContent>
        return Get(_typedStore,
                   typeof(T).FullName,
                   culture => _contentLocator.AllInCulture(AliasHelper<T>.ContentTypeAlias(), culture)
                                             .Select(x => x.As<T>())
                                             .ToList(),
                   cultures);
    }

    private bool InPreviewMode() {
        return _umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) && umbracoContext.InPreviewMode;
    }
}
