using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Web;

namespace N3O.Umbraco.Llms;

public class LlmsSettingsRemoved :
    INotificationAsyncHandler<ContentMovedToRecycleBinNotification>,
    INotificationAsyncHandler<ContentUnpublishedNotification> {
    private readonly IContentLocator _contentLocator;
    private readonly ILlmsTxt _llmsTxt;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public LlmsSettingsRemoved(IContentLocator contentLocator,
                               ILlmsTxt llmsTxt,
                               IUmbracoContextFactory umbracoContextFactory) {
        _contentLocator = contentLocator;
        _llmsTxt = llmsTxt;
        _umbracoContextFactory = umbracoContextFactory;
    }

    public Task HandleAsync(ContentMovedToRecycleBinNotification notification, CancellationToken cancellationToken) {
        Process(notification.MoveInfoCollection.Select(x => x.Entity));

        return Task.CompletedTask;
    }

    public Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken) {
        Process(notification.UnpublishedEntities);

        return Task.CompletedTask;
    }

    private bool OtherSettingsExist(IEnumerable<IContent> settingsBeingRemoved) {
        var beingRemovedKeys = settingsBeingRemoved.Select(x => x.Key).ToList();

        using (_umbracoContextFactory.EnsureUmbracoContext()) {
            var otherSettings = _contentLocator.All(AliasHelper<LlmsSettingsContent>.ContentTypeAlias())
                                               .ExceptWhere(x => beingRemovedKeys.Contains(x.Key))
                                               .ToList();

            return otherSettings.Any();
        }
    }

    private void Process(IEnumerable<IContent> entities) {
        var alias = AliasHelper<LlmsSettingsContent>.ContentTypeAlias();
        var settingsBeingRemoved = entities.Where(x => x.ContentType.Alias == alias).ToList();

        if (settingsBeingRemoved.Any() && !OtherSettingsExist(settingsBeingRemoved)) {
            _llmsTxt.Remove();
        }
    }
}
