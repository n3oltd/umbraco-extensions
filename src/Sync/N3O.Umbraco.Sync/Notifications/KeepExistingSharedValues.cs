using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using uSync.BackOffice;
using uSync.BackOffice.SyncHandlers.Handlers;
using uSync.Core;
using uSync.Core.Serialization;
using uSync.Expansions.PublishedContent;
using uSyncCoreConstants = uSync.Core.uSyncConstants;

namespace N3O.Umbraco.Sync;

public class KeepExistingSharedValues : INotificationAsyncHandler<uSyncImportingItemNotification> {
    private static readonly string[] EmptyCollections = ["[]", "{}"];

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<KeepExistingSharedValues> _logger;
    private readonly ISyncItemFactory _syncItemFactory;

    public KeepExistingSharedValues(IHttpContextAccessor httpContextAccessor,
                                    ILogger<KeepExistingSharedValues> logger,
                                    ISyncItemFactory syncItemFactory) {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _syncItemFactory = syncItemFactory;
    }

    public Task HandleAsync(uSyncImportingItemNotification notification, CancellationToken cancellationToken) {
        if (notification.Handler is not ContentHandler && notification.Handler is not MediaHandler) {
            return Task.CompletedTask;
        }

        if (!IsCultureScoped(notification.Item)) {
            return Task.CompletedTask;
        }

        var emptyValues = notification.Item
                                      .Element("Properties")?
                                      .Elements()
                                      .SelectMany(p => p.Elements("Value").Where(IsEmptySharedValue))
                                      .ToList();

        if (!emptyValues.HasAny()) {
            return Task.CompletedTask;
        }

        var existingProperties = GetExistingProperties(notification);

        if (existingProperties == null) {
            return Task.CompletedTask;
        }

        var keptAliases = new List<string>();

        foreach (var value in emptyValues) {
            var alias = value.Parent.Name.LocalName;
            var existingValue = existingProperties.Element(alias)?
                                                  .Elements("Value")
                                                  .FirstOrDefault(x => x.Attribute("Culture") == null);

            if (existingValue != null && !IsEmptySharedValue(existingValue)) {
                value.ReplaceWith(new XElement(existingValue));

                keptAliases.Add(alias);
            }
        }

        if (keptAliases.HasAny()) {
            _logger.LogInformation("Kept existing shared values on {Key} for {Aliases}",
                                   notification.Item.GetKey(),
                                   string.Join(", ", keptAliases));
        }

        return Task.CompletedTask;
    }

    private XElement GetExistingProperties(uSyncImportingItemNotification notification) {
        var settings = new Dictionary<string, string>();
        settings[uSyncCoreConstants.DefaultsKey] = bool.TrueString;

        var options = new SyncSerializerOptions(settings);

        if (notification.Handler is MediaHandler) {
            return Serialize(_syncItemFactory.GetSerializer<IMedia>("MediaSerializer"), notification.Item, options);
        }

        var serializerName = notification.Handler is PublishedContentHandler
                                 ? "PublishedContentSerializer"
                                 : "ContentSerializer";

        return Serialize(_syncItemFactory.GetSerializer<IContent>(serializerName), notification.Item, options);
    }

    private bool IsCultureScoped(XElement item) {
        if (item.GetCultures().HasValue()) {
            return true;
        }

        var httpContext = _httpContextAccessor.HttpContext;

        return httpContext != null && httpContext.Items.ContainsKey(SyncConstants.HttpContextItems.CultureScopedPush);
    }

    private bool IsEmptySharedValue(XElement value) {
        if (value.Attribute("Culture") != null) {
            return false;
        }

        var text = value.Value.Trim();

        return text.IsNullOrWhiteSpace() || EmptyCollections.Contains(text);
    }

    private XElement Serialize<T>(ISyncSerializer<T> serializer, XElement node, SyncSerializerOptions options)
        where T : class {
        var existing = serializer?.FindItem(node);

        if (existing == null) {
            return null;
        }

        var attempt = serializer.Serialize(existing, options);

        return attempt.Success ? attempt.Item?.Element("Properties") : null;
    }
}
