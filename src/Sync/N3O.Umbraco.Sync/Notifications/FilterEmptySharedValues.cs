using N3O.Umbraco.Extensions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using uSync.BackOffice;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;

namespace N3O.Umbraco.Sync;

public class FilterEmptySharedValues : INotificationAsyncHandler<uSyncImportingItemNotification> {
    public Task HandleAsync(uSyncImportingItemNotification notification, CancellationToken cancellationToken) {
        if (notification.Handler is not ContentHandler && notification.Handler is not MediaHandler) {
            return Task.CompletedTask;
        }

        if (notification.Item.GetCultures().IsNullOrWhiteSpace()) {
            return Task.CompletedTask;
        }

        var properties = notification.Item.Element("Properties");

        if (properties == null) {
            return Task.CompletedTask;
        }

        foreach (var property in properties.Elements().ToList()) {
            foreach (var value in property.Elements("Value").ToList()) {
                if (value.Attribute("Culture") == null && value.Value.IsNullOrWhiteSpace()) {
                    value.Remove();
                }
            }
        }

        return Task.CompletedTask;
    }
}
