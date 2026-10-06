using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Content;
using N3O.Umbraco.Email.SendGrid.Content;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Email.SendGrid;

public class SendGridEmailComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IEmailSender>(serviceProvider => {
            var contentCache = serviceProvider.GetRequiredService<IContentCache>();
            var settings = contentCache.Single<SendGridSettingsContent>();

            return new SendGridSender(settings.ApiKey, settings.SandboxMode);
        });
    }
}
