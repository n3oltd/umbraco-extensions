using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.Llms;

public class LlmsApplicationStarted : INotificationAsyncHandler<UmbracoApplicationStartedNotification> {
    private readonly ILlmsTxt _llmsTxt;

    public LlmsApplicationStarted(ILlmsTxt llmsTxt) {
        _llmsTxt = llmsTxt;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification,
                                  CancellationToken cancellationToken) {
        await _llmsTxt.PublishAsync();
    }
}
