using N3O.Umbraco.Content;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Llms;

public class LlmsSettingsPublished : INotificationAsyncHandler<ContentPublishedNotification> {
    private readonly ILlmsTxt _llmsTxt;

    public LlmsSettingsPublished(ILlmsTxt llmsTxt) {
        _llmsTxt = llmsTxt;
    }

    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken) {
        foreach (var content in notification.PublishedEntities) {
            if (content.ContentType.Alias == AliasHelper<LlmsSettingsContent>.ContentTypeAlias()) {
                await _llmsTxt.PublishAsync();
            }
        }
    }
}
