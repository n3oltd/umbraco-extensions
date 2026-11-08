namespace N3O.Umbraco.Hosting;

public class CorsSettings {
    public const string SectionName = "N3O:Cors";

    public string[] AllowedOrigins { get; set; } = [];
}
