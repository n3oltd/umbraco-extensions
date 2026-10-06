using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Content;
using N3O.Umbraco.Email.Smtp.Content;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Email.Smtp;

public class SmtpEmailComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IEmailSender>(serviceProvider => {
            var contentCache = serviceProvider.GetRequiredService<IContentCache>();
            var contentSettings = contentCache.Single<SmtpSettingsContent>();

            string host;
            int port;
            string username;
            string password;

            if (contentSettings != null) {
                host = contentSettings.Host;
                port = contentSettings.Port;
                username = contentSettings.Username;
                password = contentSettings.Password;
            } else {
                var appSettings = serviceProvider.GetRequiredService<IOptions<GlobalSettings>>().Value.Smtp;
                
                host = appSettings.Host;
                port = appSettings.Port;
                username = appSettings.Username;
                password = appSettings.Password;
            }
            
            var mimeMessageBuilder = serviceProvider.GetRequiredService<IMimeMessageBuilder>();

            return new SmtpSender(mimeMessageBuilder, host, port, username, password);
        });
    }
}
