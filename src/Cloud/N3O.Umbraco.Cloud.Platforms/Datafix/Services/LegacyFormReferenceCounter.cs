using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace N3O.Umbraco.Cloud.Platforms;

// TODO Delete along with the rest of the Datafix folder once every site has completed the migration.
public class LegacyFormReferenceCounter : ILegacyFormReferenceCounter {
    private const int PageSize = 200;
    private const int RootId = global::Umbraco.Cms.Core.Constants.System.Root;
    private const string UdiPrefix = "umb://document/";

    private static readonly Regex DocumentUdi = new(@"umb://document/([0-9a-fA-F]{32})",
                                                    RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IContentService _contentService;

    public LegacyFormReferenceCounter(IContentService contentService) {
        _contentService = contentService;
    }

    public IReadOnlyDictionary<Guid, int> CountReferences(IReadOnlyCollection<Guid> formKeys) {
        var counts = formKeys.Distinct().ToDictionary(x => x, _ => 0);

        if (counts.Count == 0) {
            return counts;
        }

        long page = 0;
        long total;

        do {
            var items = _contentService.GetPagedDescendants(RootId, page, PageSize, out total);

            foreach (var item in items) {
                foreach (var key in GetReferencedKeys(item)) {
                    if (key != item.Key && counts.ContainsKey(key)) {
                        counts[key] = counts[key] + 1;
                    }
                }
            }

            page++;
        } while (page * PageSize < total);

        return counts;
    }

    private static IReadOnlyCollection<Guid> GetReferencedKeys(IContent content) {
        var keys = new HashSet<Guid>();

        foreach (var property in content.Properties) {
            foreach (var value in property.Values) {
                Collect(value.EditedValue as string, keys);
                Collect(value.PublishedValue as string, keys);
            }
        }

        return keys;
    }

    private static void Collect(string value, ISet<Guid> keys) {
        if (string.IsNullOrEmpty(value) ||
            value.IndexOf(UdiPrefix, StringComparison.OrdinalIgnoreCase) < 0) {
            return;
        }

        foreach (Match match in DocumentUdi.Matches(value)) {
            if (Guid.TryParseExact(match.Groups[1].Value, "N", out var key)) {
                keys.Add(key);
            }
        }
    }
}
