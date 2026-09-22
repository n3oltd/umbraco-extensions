using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class WebhookPlatformsPage {
    public WebhookPlatformsPage(string pagePublishedPath, IEnumerable<string> pagePublishedPathsHistory) {
        PagePublishedPath = pagePublishedPath;
        PagePublishedPathsHistory = pagePublishedPathsHistory;
    }

    public string PagePublishedPath { get; }
    public IEnumerable<string> PagePublishedPathsHistory { get; }
}
