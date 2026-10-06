using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Email;

public class EmailComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient<IEmailBuilder, EmailBuilder>();
        builder.Services.AddTransient<IMimeMessageBuilder, MimeMessageBuilder>();
        builder.Services.AddTransient<ITemplateRenderer, TemplateRenderer>();
    }
}
