using Microsoft.AspNetCore.Html;
using N3O.Umbraco.Analytics.Content;
using N3O.Umbraco.Analytics.Models;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Pages;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Analytics.Modules;

public class GoogleTagManagerPageModule : IPageModule {
    private readonly IContentCache _contentCache;

    public GoogleTagManagerPageModule(IContentCache contentCache) {
        _contentCache = contentCache;
    }

    public bool ShouldExecute(IPublishedContent page) {
        var tagManagerSettings = _contentCache.Single<GoogleTagManagerSettingsContent>();

        return tagManagerSettings.HasValue(x => x.ContainerId);
    }

    public Task<object> ExecuteAsync(IPublishedContent page, CancellationToken cancellationToken) {
        var tagManagerSettings = _contentCache.Single<GoogleTagManagerSettingsContent>();

        var bodyCode = new HtmlString(@"
<!-- Google Tag Manager (noscript) -->
<noscript><iframe src=""https://www.googletagmanager.com/ns.html?id=" + tagManagerSettings.ContainerId + @"""
height=""0"" width=""0"" style=""display:none;visibility:hidden""></iframe></noscript>
<!-- End Google Tag Manager (noscript) -->
");
        
        var headCode = new HtmlString(@"
<!-- Google Tag Manager -->
<script>(function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':
new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],
j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=
'" + GetScriptBaseUrl(tagManagerSettings) + @"gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);
})(window,document,'script','dataLayer','" + tagManagerSettings.ContainerId + @"');</script>
<!-- End Google Tag Manager -->
");

        return Task.FromResult<object>(new GoogleTagManagerCode(bodyCode, headCode));
    }

    public string Key => AnalyticsConstants.PageModuleKeys.GoogleTagManager;

    private string GetScriptBaseUrl(GoogleTagManagerSettingsContent tagManagerSettings) {
        var tagGatewayPath = tagManagerSettings.TagGatewayPath?.Trim('/');

        if (tagGatewayPath.HasValue()) {
            return $"/{tagGatewayPath}/";
        } else {
            return "https://www.googletagmanager.com/";
        }
    }
}
