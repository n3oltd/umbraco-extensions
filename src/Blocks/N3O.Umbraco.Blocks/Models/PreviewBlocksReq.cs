using System;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models.Blocks;

namespace N3O.Umbraco.Blocks;

public class PreviewBlocksReq {
    public List<Guid> BlockKeys { get; set; } = new();
    public BlockGridValue BlockValue { get; set; }
}
