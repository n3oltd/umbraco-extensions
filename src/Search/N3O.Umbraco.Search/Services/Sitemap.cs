using AsyncKeyedLock;
using Flurl;
using Microsoft.AspNetCore.Hosting;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Features;
using N3O.Umbraco.Hosting;
using N3O.Umbraco.Search.Extensions;
using N3O.Umbraco.Search.Models;
using N3O.Umbraco.Utilities;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Search;

public class Sitemap : ISitemap {
    private const string SitemapFileName = "sitemap.xml";
    private const string SitemapFilePattern = "sitemap*.xml";

    private readonly IReadOnlyList<ISitemapEntriesProvider> _entriesProviders;
    private readonly AsyncKeyedLocker<string> _locker;
    private readonly IUrlBuilder _urlBuilder;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public Sitemap(IEnumerable<ISitemapEntriesProvider> entriesProviders,
                   AsyncKeyedLocker<string> locker,
                   IUrlBuilder urlBuilder,
                   IWebHostEnvironment webHostEnvironment) {
        _entriesProviders = entriesProviders.ApplyAttributeOrdering();
        _locker = locker;
        _urlBuilder = urlBuilder;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IReadOnlyList<SitemapEntry>> GetEntriesAsync(CancellationToken cancellationToken = default) {
        var entries = new List<SitemapEntry>();

        foreach (var provider in _entriesProviders) {
            entries.AddRange(await provider.GetEntriesAsync(cancellationToken));
        }

        return entries;
    }

    public async Task PublishAsync() {
        using (await _locker.LockAsync(LockKey.Generate<Sitemap>())) {
            var entries = await GetEntriesAsync();

            // A loading NuCache snapshot reads as an empty tree
            if (!entries.Any() && WebRoot.GetFiles(_webHostEnvironment, SitemapFilePattern).Any()) {
                return;
            }

            if (FeatureFlags.IsSet(FeatureFlags.SitemapIndex) && entries.Any()) {
                await PublishIndexAsync(entries);
            } else {
                await PublishSingleAsync(entries);
            }
        }
    }

    private async Task PublishSingleAsync(IEnumerable<SitemapEntry> entries) {
        await WebRoot.SaveTextAsync(_webHostEnvironment, SitemapFileName, entries.ToXml());

        PruneStaleFiles(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { SitemapFileName });
    }

    private async Task PublishIndexAsync(IEnumerable<SitemapEntry> entries) {
        var baseUrl = GetBaseUrl();

        // Without a base URL the index cannot reference its children, so fall back to the
        // single urlset, which needs no absolute URL.
        if (!baseUrl.HasValue()) {
            await PublishSingleAsync(entries);

            return;
        }

        var writtenFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var indexEntries = new List<SitemapIndexEntry>();

        var groups = entries.GroupBy(x => (x.Section, LanguageSlug: x.Culture.ToHrefLang()));

        foreach (var group in groups) {
            var fileName = GetChildFileName(group.Key.Section, group.Key.LanguageSlug);
            var groupEntries = group.ToList();

            await WebRoot.SaveTextAsync(_webHostEnvironment, fileName, groupEntries.ToXml());

            writtenFileNames.Add(fileName);
            indexEntries.Add(new SitemapIndexEntry(Url.Combine(baseUrl, fileName), GetLastModified(groupEntries)));
        }

        await WebRoot.SaveTextAsync(_webHostEnvironment, SitemapFileName, indexEntries.ToSitemapIndexXml());
        writtenFileNames.Add(SitemapFileName);

        PruneStaleFiles(writtenFileNames);
    }

    private string GetBaseUrl() {
        try {
            return _urlBuilder.Root().ToString();
        } catch {
            return null;
        }
    }

    private void PruneStaleFiles(ISet<string> writtenFileNames) {
        foreach (var file in WebRoot.GetFiles(_webHostEnvironment, SitemapFilePattern)) {
            if (!writtenFileNames.Contains(file.Name)) {
                WebRoot.DeleteFile(_webHostEnvironment, file.Name);
            }
        }
    }

    private static string GetChildFileName(string section, string languageSlug) {
        if (languageSlug.HasValue()) {
            return $"sitemap_{section}_{languageSlug}.xml";
        } else {
            return $"sitemap_{section}.xml";
        }
    }

    private static LocalDate? GetLastModified(IEnumerable<SitemapEntry> entries) {
        var dates = entries.Where(x => x.LastModified.HasValue()).Select(x => x.LastModified.Value).ToList();

        if (dates.Any()) {
            return dates.Max();
        } else {
            return null;
        }
    }
}
