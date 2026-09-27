using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Media;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Content;

public class ContentComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        RegisterAll(t => t.ImplementsInterface<IContentRenderabilityFilter>(),
                    t => builder.Services.AddScoped(typeof(IContentRenderabilityFilter), t));
        
        RegisterAll(t => t.ImplementsInterface<IContentVisibilityFilter>(),
                    t => builder.Services.AddScoped(typeof(IContentVisibilityFilter), t));
    
        builder.Services.AddTransient<IContentEditor, ContentEditor>();
        builder.Services.AddSingleton<IContentCache, ContentCache>();
        builder.Services.AddSingleton<IContentHelper, ContentHelper>();
        builder.Services.AddSingleton<IContentLocator, ContentLocator>();
        builder.Services.AddTransient<IContentVisibility, ContentVisibility>();
        builder.Services.AddTransient<ICultureSeeder, CultureSeeder>();
        builder.Services.AddSingleton<IMediaLocator, MediaLocator>();
        builder.Services.AddScoped<IPageModeAccessor, PageModeAccessor>();
        
        RegisterAll(t => t.ImplementsInterface<IContentAccessControl>(),
                    t => {
                        builder.Services.AddTransient(typeof(IContentAccessControl), t);
                        builder.Services.AddTransient(t, t);
                    });
    
        RegisterAll(t => t.ImplementsInterface<IContentValidator>(),
                    t => builder.Services.AddTransient(typeof(IContentValidator), t));
        
        RegisterAll(t => t.ImplementsInterface<IPropertyBuilder>(),
                    t => builder.Services.AddTransient(t));

        builder.Components().Append<CultureSeedingMigrationsComponent>();
    }
}
