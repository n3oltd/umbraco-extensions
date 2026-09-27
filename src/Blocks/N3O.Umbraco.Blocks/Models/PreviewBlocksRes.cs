using System.Collections.Generic;

namespace N3O.Umbraco.Blocks;

public class PreviewBlocksRes {
    // A block that could not be rendered is still answered with markup, an error banner, so the blocks that
    // failed have to be named separately for the editor to know not to keep one.
    public IEnumerable<string> Failed { get; set; }
    public Dictionary<string, string> Markup { get; set; }
}
