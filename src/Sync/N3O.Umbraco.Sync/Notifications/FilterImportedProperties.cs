using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using uSync.BackOffice;
using uSync.BackOffice.SyncHandlers.Handlers;

namespace N3O.Umbraco.Sync;

public class FilterImportedProperties : INotificationAsyncHandler<uSyncImportingItemNotification> {
    private readonly IEnumerable<ISyncFilter> _syncFilters;

    public FilterImportedProperties(IEnumerable<ISyncFilter> syncFilters) {
        _syncFilters = syncFilters.OrEmpty().ToList();
    }
    
    public Task HandleAsync(uSyncImportingItemNotification notification, CancellationToken cancellationToken) {
        var contentTypeAlias = notification.Item.Element("Info")?.Element("ContentType")?.Value;
        var properties = notification.Item.Element("Properties");

        if ((notification.Handler is ContentHandler || notification.Handler is MediaHandler) &&
            contentTypeAlias.HasValue() &&
            properties != null) {
            var filters = _syncFilters.Where(x => x.IsFilter(contentTypeAlias));

            foreach (var filter in filters) {
                foreach (var element in properties.Elements().ToList()) {
                    if (!filter.ShouldImport(element.Name.LocalName)) {
                        element.Remove();
                    }
                }
            }
        }

        return Task.CompletedTask;
    }
}
