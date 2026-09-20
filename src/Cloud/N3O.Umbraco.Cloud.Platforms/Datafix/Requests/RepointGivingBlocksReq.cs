using System.Collections.Generic;

namespace N3O.Umbraco.Cloud.Platforms.Models;

public class RepointGivingBlocksReq {
    public IReadOnlyList<string> ContentTypeAliases { get; set; } = [];
    public string PropertyAlias { get; set; }
    public bool Preview { get; set; }
}
