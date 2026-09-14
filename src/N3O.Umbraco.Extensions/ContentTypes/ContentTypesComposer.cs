using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.ContentTypes;

public class ContentTypesComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddTransient<IContentTypeEditor, ContentTypeEditor>();

        RegisterAll(t => t.ImplementsInterface<IPropertyTypeBuilder>(),
                    t => builder.Services.AddTransient(t));
    }
}
