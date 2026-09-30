using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class WebhookPlatformsPage {
    public WebhookPlatformsPage(string pagePublishedPath,
                                IEnumerable<string> pagePublishedPathsHistory,
                                IEnumerable<string> affectedPublishedPaths) {
        PagePublishedPath = pagePublishedPath;
        PagePublishedPathsHistory = pagePublishedPathsHistory;
        AffectedPublishedPaths = affectedPublishedPaths;
    }

    public string PagePublishedPath { get; }
    public IEnumerable<string> PagePublishedPathsHistory { get; }
    public IEnumerable<string> AffectedPublishedPaths { get; }
}
