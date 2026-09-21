using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class RewriteGivingBlocksReq {
    public bool Preview { get; set; }
    public string ItemContentTypeAlias { get; set; } = PlatformsConstants.DonationFormItems.Campaign;
    public IEnumerable<string> PropertyAliases { get; set; } = [];
}
