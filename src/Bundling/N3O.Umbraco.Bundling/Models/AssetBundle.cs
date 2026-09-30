using System.Collections.Generic;

namespace N3O.Umbraco.Bundling.Models;

public class AssetBundle {
    public List<AssetReference> Css { get; set; } = new();
    public List<AssetReference> Js { get; set; } = new();
}
