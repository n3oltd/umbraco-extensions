using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Cloud.Platforms.Hosting;
using N3O.Umbraco.Cloud.Platforms.TagHelpers;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace N3O.Umbraco.Cloud.Platforms;

public class PlatformsComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Components().Append<PlatformsSchemaComponent>();

        builder.Services.AddOpenApiDocument(PlatformsConstants.BackOfficeApiName);
        builder.Services.AddOpenApiDocument(PlatformsConstants.DevToolsApiName);
        builder.Services.AddSingleton<ICampaignAccessor, CampaignAccessor>();
        builder.Services.AddSingleton<ICampaignIdAccessor, CampaignIdAccessor>();
        builder.Services.AddTransient<ICrowdfundingCampaignContentCopier, CrowdfundingCampaignContentCopier>();
        builder.Services.AddSingleton<INisab, Nisab>();
        builder.Services.AddTransient<IPlatformsContentTypeSeeder, PlatformsContentTypeSeeder>();
        builder.Services.AddTransient<IPlatformsDataTypeSeeder, PlatformsDataTypeSeeder>();
        builder.Services.AddSingleton<IPlatformsPageAccessor, PlatformsPageAccessor>();
        builder.Services.AddTransient<IPlatformsSchemaAudit, PlatformsSchemaAudit>();
        builder.Services.AddSingleton<ITagHelperComponent, PlatformsTagHelperComponent>();
        
        builder.Services.AddScoped<PlatformsCdnFailureMiddleware>();
        builder.Services.AddScoped<PlatformsTemplatesMiddleware>();
        
        RegisterAll(t => t.ImplementsInterface<ICampaignIdProvider>(),
                    t => builder.Services.AddTransient(typeof(ICampaignIdProvider), t));
        
        RegisterAll(t => t.ImplementsInterface<IPlatformsPageContentPublisher>(),
                    t => builder.Services.AddTransient(typeof(IPlatformsPageContentPublisher), t));
        
        RegisterAll(t => t.ImplementsInterface<IPreviewHtmlGenerator>(),
                    t => builder.Services.AddTransient(typeof(IPreviewHtmlGenerator), t));
        
        builder.Services.Configure<UmbracoPipelineOptions>(opt => {
            var filter = new UmbracoPipelineFilter(nameof(PlatformsCdnFailureMiddleware));

            filter.PrePipeline = app => {
                var runtimeState = app.ApplicationServices.GetRequiredService<IRuntimeState>();

                if (runtimeState.Level == RuntimeLevel.Run) {
                    app.UseMiddleware<PlatformsCdnFailureMiddleware>();
                }
            };

            opt.AddFilter(filter);
        });

        builder.Services.Configure<UmbracoPipelineOptions>(opt => {
            var filter = new UmbracoPipelineFilter(nameof(PlatformsTemplatesMiddleware));
            
            filter.PostPipeline = app => {
                var runtimeState = app.ApplicationServices.GetRequiredService<IRuntimeState>();

                if (runtimeState.Level == RuntimeLevel.Run) {
                    app.UseMiddleware<PlatformsTemplatesMiddleware>();
                }
            };

            opt.AddFilter(filter);
        });
    }
}
