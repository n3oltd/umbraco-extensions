using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Cloud.Hosting;
using N3O.Umbraco.Cloud.Options;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace N3O.Umbraco.Cloud;

[ComposeAfter(typeof(LocalizationComposer))]
public class CloudComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddOpenApiDocument(CloudConstants.BackOfficeApiName);
        builder.Services.Configure<CdnCacheOptions>(builder.Config.GetSection(CloudConstants.Configuration.CdnCacheSection));

        builder.Services.AddScoped(typeof(ClientFactory<>));
        builder.Services.AddSingleton<ICdnClient, CdnClient>();
        builder.Services.AddSingleton<ICloudUrl, CloudUrl>();
        builder.Services.AddSingleton<ILocalizationSettingsAccessor, PublishedLocalizationSettingsAccessor>();
        builder.Services.AddSingleton<IOrganizationInfoAccessor, OrganizationInfoAccessor>();
        builder.Services.AddSingleton<ISubscriptionAccessor, SubscriptionAccessor>();
        
        RegisterMiddleware(builder);
    }
    
    private void RegisterMiddleware(IUmbracoBuilder builder) {
        builder.Services.AddScoped<ConnectMiddleware>();
        
        builder.Services.Configure<UmbracoPipelineOptions>(opt => {
            var filter = new UmbracoPipelineFilter(nameof(ConnectMiddleware));

            filter.PrePipeline = app => {
                var runtimeState = app.ApplicationServices.GetRequiredService<IRuntimeState>();

                if (runtimeState.Level == RuntimeLevel.Run) {
                    app.UseMiddleware<ConnectMiddleware>();
                }
            };

            opt.AddFilter(filter);
        });
    }
}