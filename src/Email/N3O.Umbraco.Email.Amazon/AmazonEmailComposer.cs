using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Content;
using N3O.Umbraco.Email.Amazon.Content;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Email.Amazon;

public class AmazonEmailComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IEmailSender>(serviceProvider => {
            var contentCache = serviceProvider.GetRequiredService<IContentCache>();
            var settings = contentCache.Single<AmazonSettingsContent>();
            var mimeMessageBuilder = serviceProvider.GetRequiredService<IMimeMessageBuilder>();

            return new AmazonSender(mimeMessageBuilder, settings.AccessKey, settings.SecretKey, settings.RegionCode);
        });
    }
}
