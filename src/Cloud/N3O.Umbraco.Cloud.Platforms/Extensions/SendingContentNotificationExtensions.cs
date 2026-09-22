using Flurl;
using N3O.Umbraco.Content;
using N3O.Umbraco.Utilities;
using System.Collections.Generic;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;

namespace N3O.Umbraco.Cloud.Platforms.Extensions;

public static class SendingContentNotificationExtensions {
    public static void SetPlatformsUrls(this SendingContentNotification notification,
                                        IContentCache contentCache,
                                        string path) {
        var urlSettings = contentCache.Single<UrlSettingsContent>();

        if (urlSettings == null) {
            return;
        }

        var stagingUrl = new Url(urlSettings.StagingBaseUrl).AppendPathSegment(path);
        var productionUrl = new Url(urlSettings.ProductionBaseUrl).AppendPathSegment(path);

        var urls = new List<UrlInfo>();
        urls.Add(new UrlInfo(stagingUrl, true, null));
        urls.Add(new UrlInfo(productionUrl, true, null));

        notification.Content.Urls = urls.ToArray();
    }
}