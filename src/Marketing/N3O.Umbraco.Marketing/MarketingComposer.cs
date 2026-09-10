using Microsoft.Extensions.DependencyInjection;
using N3O.Umbraco.Composing;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Marketing.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Engage.Infrastructure.Analytics.Collection.Extractors;
using Umbraco.Engage.Infrastructure.Analytics.Processing.Extractors;
using Umbraco.Extensions;

namespace N3O.Umbraco.Marketing;

[ComposeAfter(typeof(AnalyticsExtractorsComposer))]
[ComposeAfter(typeof(AnalyticsProcessingExtractorsComposer))]
public class MarketingComposer : Composer {
    public override void Compose(IUmbracoBuilder builder) {
        builder.Services.AddOpenApiDocument(MarketingConstants.ApiName);
        
        builder.Services.AddUnique<IHttpContextIpAddressExtractor, EngageIpAddressExtractor>();
        builder.Services.AddUnique<IRawPageviewLocationExtractor, EngageLocationExtractor>();
        builder.Services.AddTransient<IMarketingExport, MarketingExport>();
    }
}
