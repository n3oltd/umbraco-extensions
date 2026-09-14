using N3O.Umbraco.Bundling.Models;
using System.Collections.Generic;

namespace N3O.Umbraco.Bundling;

public interface IAssetManifest {
    IReadOnlyList<AssetReference> GetCss(string bundle);
    IReadOnlyList<AssetReference> GetJs(string bundle);
}
