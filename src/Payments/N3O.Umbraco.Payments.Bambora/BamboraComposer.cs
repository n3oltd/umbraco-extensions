using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Payments.Bambora.Clients;
using N3O.Umbraco.Payments.Bambora.Content;
using N3O.Umbraco.Payments.Bambora.Models;
using Refit;
using Umbraco.Cms.Core.DependencyInjection;

namespace N3O.Umbraco.Payments.Bambora;

public class BamboraComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddOpenApiDocument(BamboraConstants.ApiName);

        builder.Services.AddSingleton<BamboraApiSettings>(serviceProvider => {
            var contentCache = serviceProvider.GetRequiredService<IContentCache>();
            var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var apiSettings = GetApiSettings(contentCache, webHostEnvironment);

            return apiSettings;
        });
        
        builder.Services.AddTransient<IBamboraPaymentsClient>(serviceProvider => {
            var apiSettings = serviceProvider.GetRequiredService<BamboraApiSettings>();
            IBamboraPaymentsClient client = null;

            if (apiSettings != null) {
                var refitSettings = new RefitSettings();
                refitSettings.ContentSerializer = new NewtonsoftJsonContentSerializer();

                refitSettings.HttpMessageHandlerFactory =
                    () => new AuthorizationHandler(apiSettings.MerchantId, apiSettings.PaymentPasscode);

                client = RestService.For<IBamboraPaymentsClient>(BamboraConstants.ApiBaseUrl, refitSettings);
            }
            
            return client;
        });
        
        builder.Services.AddTransient<IBamboraProfilesClient>(serviceProvider => {
            var apiSettings = serviceProvider.GetRequiredService<BamboraApiSettings>();
            IBamboraProfilesClient client = null;

            if (apiSettings != null) {
                var refitSettings = new RefitSettings();
                refitSettings.ContentSerializer = new NewtonsoftJsonContentSerializer();

                refitSettings.HttpMessageHandlerFactory =
                    () => new AuthorizationHandler(apiSettings.MerchantId, apiSettings.ProfilePasscode);

                client = RestService.For<IBamboraProfilesClient>(BamboraConstants.ApiBaseUrl, refitSettings);
            }
            
            return client;
        });
    }

    private BamboraApiSettings GetApiSettings(IContentCache contentCache, IWebHostEnvironment webHostEnvironment) {
        var settings = contentCache.Single<BamboraSettingsContent>();

        if (settings != null) {
            if (webHostEnvironment.IsProduction()) {
                return new BamboraApiSettings(settings.ProductionMerchantId,
                                              settings.ProductionPaymentsPasscode,
                                              settings.ProductionProfilesPasscode);
            } else {
                return new BamboraApiSettings(settings.StagingMerchantId,
                                              settings.StagingPaymentsPasscode,
                                              settings.StagingProfilesPasscode);
            }
        }

        return null;
    }
}
