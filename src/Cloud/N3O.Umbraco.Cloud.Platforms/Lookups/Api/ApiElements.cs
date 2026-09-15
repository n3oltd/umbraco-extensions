using Microsoft.Extensions.Logging;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Cloud.Extensions;
using N3O.Umbraco.Cloud.Lookups;
using N3O.Umbraco.Cloud.Platforms.Clients;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Lookups;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace N3O.Umbraco.Cloud.Platforms.Lookups;

[Order(int.MaxValue)]
public class ApiElements : ApiLookupsCollection<Element> {
    private static readonly ConcurrentDictionary<string, bool> UnknownKindElementIds = new();

    private readonly ICdnClient _cdnClient;
    private readonly ILogger<ApiElements> _logger;

    public ApiElements(ICdnClient cdnClient, ILogger<ApiElements> logger) {
        _cdnClient = cdnClient;
        _logger = logger;
    }
    
    protected override async Task<IReadOnlyList<Element>> FetchAsync(CancellationToken cancellationToken) {
        var publishedElements = await _cdnClient.DownloadSubscriptionContentAsync<PublishedElements>(SubscriptionFiles.Elements,
                                                                                                     JsonSerializers.JsonProvider,
                                                                                                     cancellationToken);

        var elements = new List<Element>();

        foreach (var publishedElement in publishedElements.OrEmpty(x => x.Elements)) {
            if (publishedElement.ElementKind == null) {
                if (UnknownKindElementIds.TryAdd(publishedElement.Id, true)) {
                    _logger.LogError("Skipping published element {ElementId} as its kind is not known to this site",
                                     publishedElement.Id);
                }

                continue;
            }

            var element = new Element(publishedElement.Id,
                                      publishedElement.Name,
                                      null,
                                      publishedElement.ElementKind.Value,
                                      publishedElement.EmbedCode);
            
            elements.Add(element);
        }

        return elements;
    }

    public override async Task<Element> FindByIdAsync(string id, CancellationToken cancellationToken = default) {
        if (id.Contains("_")) {
            id = id.Replace("_", "/");
        } else if (Guid.TryParseExact(id, "N", out _)) {
            var elementKinds = Enum.GetValues(typeof(ElementKind)).Cast<ElementKind>();

            foreach (var elementKind in elementKinds) {
                var element = await base.FindByIdAsync($"{elementKind.ToEnumString()}/{id}", cancellationToken);

                if (element != null) {
                    id = element.Id;
                    
                    break;
                }
            }
        }

        return await base.FindByIdAsync(id, cancellationToken);
    }

    protected override TimeSpan ReloadInterval => TimeSpan.FromMinutes(1);
}