using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Cropper;

public class CropperComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<IImageCropper, ImageCropper>();
        builder.Services.AddSingleton<ICropSuggestions, CropSuggestions>();

        builder.Services.AddOpenApiDocument(CropperConstants.ApiName);

        RegisterAll(t => t.ImplementsInterface<ICropSuggester>(),
                    t => builder.Services.AddSingleton(typeof(ICropSuggester), t));
    }
}
