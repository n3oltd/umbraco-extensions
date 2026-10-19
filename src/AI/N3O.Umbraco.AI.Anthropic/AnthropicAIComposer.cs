using N3O.Umbraco.Composing;
using Umbraco.AI.Extensions;
using Umbraco.AI.Startup.Configuration;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.AI.Anthropic;

[ComposeAfter(typeof(UmbracoAIComposer))]
public class AnthropicAIComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.AIChatMiddleware().Append<BudgetExceededChatMiddleware>();
    }
}
