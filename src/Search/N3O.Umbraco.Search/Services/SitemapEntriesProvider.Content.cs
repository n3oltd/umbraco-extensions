using N3O.Umbraco.Constants;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Search.Extensions;
using N3O.Umbraco.Search.Models;
using NodaTime.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Search;

public class ContentSitemapEntriesProvider : ISitemapEntriesProvider {
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly ILocalizationSettingsAccessor _localizationSettingsAccessor;
    private readonly IContentLocator _contentLocator;
    private readonly IContentVisibility _contentVisibility;

    public ContentSitemapEntriesProvider(IUmbracoContextFactory umbracoContextFactory,
                                         ILocalizationSettingsAccessor localizationSettingsAccessor,
                                         IContentLocator contentLocator,
                                         IContentVisibility contentVisibility) {
        _umbracoContextFactory = umbracoContextFactory;
        _localizationSettingsAccessor = localizationSettingsAccessor;
        _contentLocator = contentLocator;
        _contentVisibility = contentVisibility;
    }
    
    public Task<IEnumerable<SitemapEntry>> GetEntriesAsync(CancellationToken cancellationToken = default) {
        using (_umbracoContextFactory.EnsureUmbracoContext()) {
            var localizationSettings = _localizationSettingsAccessor.GetSettings();

            var entries = _contentLocator.AllInCulture(null, Cultures.All)
                                         .Where(x => _contentVisibility.IsVisible(x))
                                         .SelectMany(x => GetSitemapEntries(x, localizationSettings.DefaultCultureCode))
                                         .ToList();

            return Task.FromResult<IEnumerable<SitemapEntry>>(entries);
        }
    }

    private IEnumerable<SitemapEntry> GetSitemapEntries(IPublishedContent content, string defaultCultureCode) {
        var section = SitemapSections.GetSection(content.ContentType.Alias);
        var lastModified = content.UpdateDate.ToLocalDateTime().Date;

        var publishedCultureCodes = content.OrEmpty(x => x.Cultures)
                                           .Select(x => x.Key)
                                           .Where(x => x.HasValue())
                                           .ToList();

        if (!publishedCultureCodes.Any()) {
            var url = content.AbsoluteUrl();

            if (IsRoutable(url)) {
                yield return new SitemapEntry(url, null, section, lastModified, null);
            }

            yield break;
        }

        var cultureUrls = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var cultureCode in publishedCultureCodes) {
            var url = content.AbsoluteUrl(cultureCode);

            if (IsRoutable(url)) {
                cultureUrls[cultureCode] = url;
            }
        }

        if (!cultureUrls.Any()) {
            yield break;
        }

        if (cultureUrls.Count == 1) {
            var only = cultureUrls.First();

            yield return new SitemapEntry(only.Value, only.Key, section, lastModified, null);

            yield break;
        }

        var xDefaultUrl = cultureUrls.ContainsKey(defaultCultureCode)
                              ? cultureUrls[defaultCultureCode]
                              : cultureUrls.Values.First();

        foreach (var cultureCode in cultureUrls.Keys) {
            var alternateUrls = new Dictionary<string, string>(cultureUrls, StringComparer.InvariantCultureIgnoreCase);
            alternateUrls[SitemapEntryExtensions.XDefault] = xDefaultUrl;

            yield return new SitemapEntry(cultureUrls[cultureCode], cultureCode, section, lastModified, alternateUrls);
        }
    }

    private static bool IsRoutable(string url) {
        return url.HasValue() && !url.EndsWith("#");
    }
}
