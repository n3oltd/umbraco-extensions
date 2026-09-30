namespace N3O.Umbraco.Bundling;

public class BundlingSettings {
    public const string SectionName = "N3O:Bundling";

    public string BaseUrl { get; set; }
    public string ManifestPath { get; set; } = "assets/bundles/assets-manifest.json";
    public bool ServeSourceMaps { get; set; }
}
