using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Payments.Opayo.Clients;
using N3O.Umbraco.Payments.Opayo.Content;
using N3O.Umbraco.Payments.Opayo.Models;
using N3O.Umbraco.Utilities;
using Refit;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Payments.Opayo;

public class OpayoComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddOpenApiDocument(OpayoConstants.ApiName);

        builder.Services.AddSingleton<OpayoApiSettings>(serviceProvider => {
            var contentCache = serviceProvider.GetRequiredService<IContentCache>();
            var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var apiSettings = GetApiSettings(contentCache, webHostEnvironment);
            
            return apiSettings;
        });

        builder.Services.AddTransient<IOpayoClient>(serviceProvider => {
            var apiSettings = serviceProvider.GetRequiredService<OpayoApiSettings>();
            IOpayoClient client = null;

            if (apiSettings != null) {
                var refitSettings = new RefitSettings();
                refitSettings.ContentSerializer = new NewtonsoftJsonContentSerializer();

                refitSettings.HttpMessageHandlerFactory =
                    () => new AddBasicAuthorizationHandler(apiSettings.IntegrationKey,
                                                           apiSettings.IntegrationPassword);

                client = RestService.For<IOpayoClient>(apiSettings.BaseUrl, refitSettings);
            }
            
            return client;
        });

        builder.Services.AddTransient<IOpayoHelper, OpayoHelper>();
    }
    
    private OpayoApiSettings GetApiSettings(IContentCache contentCache, IWebHostEnvironment webHostEnvironment) {
        var settings = contentCache.Single<OpayoSettingsContent>();
        
        if (settings != null) {
            if (webHostEnvironment.IsProduction()) {
                return new OpayoApiSettings("https://live.opayo.eu.elavon.com",
                                            settings.ProductionIntegrationKey,
                                            settings.ProductionIntegrationPassword,
                                            settings.ProductionVendorName);
            } else {
                return new OpayoApiSettings("https://sandbox.opayo.eu.elavon.com",
                                            settings.StagingIntegrationKey,
                                            settings.StagingIntegrationPassword,
                                            settings.StagingVendorName);
            }
        }

        return null;
    }
}
