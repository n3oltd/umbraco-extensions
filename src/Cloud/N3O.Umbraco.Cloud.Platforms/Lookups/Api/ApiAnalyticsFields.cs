using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

[Order(int.MaxValue)]
public class ApiAnalyticsFields : ApiLookupsCollection<AnalyticsField> {
    private static readonly IReadOnlyList<string> BuiltInKeyPrefixes = ["people::", "platforms::"];

    private readonly ICdnClient _cdnClient;

    public ApiAnalyticsFields(ICdnClient cdnClient) {
        _cdnClient = cdnClient;
    }

    protected override async Task<IReadOnlyList<AnalyticsField>> FetchAsync(CancellationToken cancellationToken) {
        var publishedTagDefinitions =
            await _cdnClient.DownloadSubscriptionContentAsync<PublishedTagDefinitions>(SubscriptionFiles.TagDefinitions,
                                                                                       JsonSerializers.Simple,
                                                                                       cancellationToken);

        var analyticsFields = new List<AnalyticsField>();

        foreach (var publishedTagDefinition in publishedTagDefinitions.OrEmpty(x => x.Definitions)
                                                                      .Where(x => x.Scope == TagScopes.AnalyticsField &&
                                                                                  !x.AutoPopulated)) {
            var key = publishedTagDefinition.Keys.First();

            if (IsBuiltInKey(key)) {
                continue;
            }

            var analyticsField = new AnalyticsField(publishedTagDefinition.Id,
                                                    publishedTagDefinition.Name,
                                                    publishedTagDefinition.Description,
                                                    key);

            analyticsFields.Add(analyticsField);
        }

        return analyticsFields;
    }

    private bool IsBuiltInKey(string key) {
        return BuiltInKeyPrefixes.Any(x => key.StartsWith(x, StringComparison.InvariantCultureIgnoreCase));
    }
}
