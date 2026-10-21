using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.AI.Connections;
using N3O.Umbraco.AI.Notifications;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using Umbraco.AI.Extensions;
using Umbraco.AI.Startup.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace N3O.Umbraco.AI;

[ComposeAfter(typeof(UmbracoAIComposer))]
public class AIComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        RegisterAll(t => t.IsSubclassOfType(typeof(AIConnectionDefinition)),
                    t => builder.Services.AddTransient(typeof(AIConnectionDefinition), t));

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, AIConnectionsApplicationStarted>();

        builder.AIChatMiddleware().Append<BudgetExceededChatMiddleware>();

        RegisterAll(t => t.ImplementsInterface<IBudgetExceededMatcher>(),
                    t => builder.Services.AddTransient(typeof(IBudgetExceededMatcher), t));
    }
}
