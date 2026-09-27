using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Marketing.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Engage.Common.Composing;
using Umbraco.Engage.Infrastructure.Analytics.Collection;
using Umbraco.Engage.Infrastructure.Analytics.Collection.Extractors;
using Umbraco.Engage.Infrastructure.Analytics.Processing.Extractors;
using Umbraco.Extensions;

namespace N3O.Umbraco.Marketing;

[ComposeAfter(typeof(AnalyticsExtractorsComposer))]
[ComposeAfter(typeof(AnalyticsProcessingExtractorsComposer))]
[ComposeAfter(typeof(UmbracoEngageApplicationComposer))]
public class MarketingComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddOpenApiDocument(MarketingConstants.ApiName);
        
        builder.Services.AddUnique<IHttpContextIpAddressExtractor, EngageIpAddressExtractor>();
        builder.Services.AddUnique<IRawPageviewLocationExtractor, EngageLocationExtractor>();
        builder.Services.AddTransient<IMarketingExport, MarketingExport>();

        if (BrowserEngage.IsEnabled()) {
            RegisterBrowserEngage(builder);
        }
    }

    private void RegisterBrowserEngage(IUmbracoBuilder builder) {
        builder.Services.AddSingleton<AnalyticsRequestFilter>();
        builder.Services.AddUnique<IAnalyticsRequestFilter, BrowserEngageRequestFilter>();

        builder.Services.Configure<MvcOptions>(options => options.Filters.Add<ServerEngageFilter>());
    }
}
