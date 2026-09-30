using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models.Blocks;

namespace N3O.Umbraco.Blocks;

public class PreviewBlocksReq {
    public IEnumerable<Guid> BlockKeys { get; set; }
    public BlockGridValue BlockValue { get; set; }
}
