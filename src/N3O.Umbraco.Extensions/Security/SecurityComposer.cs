using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Security;

public class SecurityComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IAuthorization, Authorization>();
        builder.Services.AddTransient<IBackofficeUser, BackofficeUser>();
        builder.Services.AddSingleton<IRenderFailures, RenderFailures>();
    }
}
